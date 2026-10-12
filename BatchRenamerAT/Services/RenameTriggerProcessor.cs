using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BatchRenamerAT.Services;

// Applies an ordered trigger list to item names. One processor instance covers one pass over the
// item list, so sequence numbers keep counting across the items it processes.
public sealed class RenameTriggerProcessor
{
    public const int MaximumDigits = 12;

    private static readonly TimeSpan s_regexMatchTimeout = TimeSpan.FromSeconds(1);

    private readonly List<CompiledTrigger> _compiledTriggers;

    // Throws ArgumentException when a trigger holds an invalid regular expression.
    public RenameTriggerProcessor(IEnumerable<RenameTrigger> triggers) => _compiledTriggers = [.. triggers.Where(trigger => trigger.IsEnabled).Select(trigger => new CompiledTrigger(trigger))];

    public string Apply(string name, bool isDirectory)
    {
        var currentName = name;
        foreach (var compiledTrigger in _compiledTriggers) currentName = compiledTrigger.Apply(currentName, isDirectory);
        return currentName;
    }

    public static Regex CreateRegex(string pattern, bool useRegularExpression, bool isCaseSensitive, bool isRightToLeft)
    {
        var regexOptions = RegexOptions.CultureInvariant;
        if (!isCaseSensitive) regexOptions |= RegexOptions.IgnoreCase;
        if (isRightToLeft) regexOptions |= RegexOptions.RightToLeft;
        return new Regex(useRegularExpression ? pattern : Regex.Escape(pattern), regexOptions, s_regexMatchTimeout);
    }

    public static string FormatSequenceNumber(long number, int digits)
    {
        var format = "D" + Math.Clamp(digits, 1, MaximumDigits).ToString(CultureInfo.InvariantCulture);
        return number < 0 ? "-" + (-number).ToString(format, CultureInfo.InvariantCulture) : number.ToString(format, CultureInfo.InvariantCulture);
    }

    public static string ConvertCase(string text, CaseConversionMode caseConversionMode) => caseConversionMode switch
    {
        CaseConversionMode.Uppercase => text.ToUpperInvariant(),
        CaseConversionMode.Lowercase => text.ToLowerInvariant(),
        CaseConversionMode.TitleCase => ConvertToTitleCase(text),
        _ => ConvertToSentenceCase(text)
    };

    public static string GetScopeText(string name, bool isDirectory, NameScope scope) => scope switch
    {
        NameScope.FileName => SplitName(name, isDirectory).BaseName,
        NameScope.Extension => SplitName(name, isDirectory).Extension,
        _ => name
    };

    private static string ReplaceScopeText(string name, bool isDirectory, NameScope scope, string scopeText)
    {
        if (scope == NameScope.FullName) return scopeText;

        var (baseName, extension) = SplitName(name, isDirectory);
        return scope == NameScope.FileName ? CombineName(scopeText, extension) : CombineName(baseName, scopeText);
    }

    // Folders have no extension; for files the extension is the text after the last dot, and a
    // leading dot (".gitignore") belongs to the name.
    private static (string BaseName, string Extension) SplitName(string name, bool isDirectory)
    {
        if (isDirectory) return (name, "");

        var dotIndex = name.LastIndexOf('.');
        return dotIndex <= 0 ? (name, "") : (name[..dotIndex], name[(dotIndex + 1)..]);
    }

    private static string CombineName(string baseName, string extension) => extension.Length == 0 ? baseName : $"{baseName}.{extension}";

    // Capitalizes the first letter of every word; apostrophes stay inside the word ("it's").
    private static string ConvertToTitleCase(string text)
    {
        var stringBuilder = new StringBuilder(text.Length);
        var isWordStart = true;
        foreach (var character in text)
        {
            stringBuilder.Append(isWordStart ? char.ToUpperInvariant(character) : char.ToLowerInvariant(character));
            isWordStart = !char.IsLetterOrDigit(character) && character != '\'';
        }

        return stringBuilder.ToString();
    }

    private static string ConvertToSentenceCase(string text)
    {
        var characters = text.ToLowerInvariant().ToCharArray();
        var firstLetterIndex = Array.FindIndex(characters, char.IsLetter);
        if (firstLetterIndex >= 0) characters[firstLetterIndex] = char.ToUpperInvariant(characters[firstLetterIndex]);
        return new string(characters);
    }

    // A trigger with its regular expressions built once and its own sequence number counter.
    private sealed class CompiledTrigger
    {
        private readonly RenameTrigger _trigger;
        private readonly Regex? _findRegex;
        private readonly Regex? _conditionRegex;
        private long _appliedCount;

        public CompiledTrigger(RenameTrigger trigger)
        {
            _trigger = trigger;
            if (trigger is ReplaceTrigger { Target: ReplaceTarget.SpecificText } replaceTrigger && replaceTrigger.FindText.Length > 0) _findRegex = CreateRegex(replaceTrigger.FindText, replaceTrigger.UseRegularExpression, replaceTrigger.IsCaseSensitive, replaceTrigger.Occurrence == MatchOccurrence.Last);
            if (trigger.Condition is { Operator: ConditionOperator.MatchesRegularExpression } condition) _conditionRegex = CreateRegex(condition.Text, true, condition.IsCaseSensitive, false);
        }

        public string Apply(string name, bool isDirectory)
        {
            if (_trigger.Scope == NameScope.Extension && isDirectory) return name;
            if (!IsConditionSatisfied(name, isDirectory)) return name;

            var scopeText = GetScopeText(name, isDirectory, _trigger.Scope);
            var (transformedText, isApplied) = Transform(scopeText);
            if (!isApplied) return name;

            // The sequence number only advances for items the trigger actually changed.
            _appliedCount++;
            return ReplaceScopeText(name, isDirectory, _trigger.Scope, transformedText);
        }

        private bool IsConditionSatisfied(string name, bool isDirectory)
        {
            if (_trigger.Condition is not { } condition) return true;

            var scopeText = GetScopeText(name, isDirectory, condition.Scope);
            var comparison = condition.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var isMatched = condition.Operator switch
            {
                ConditionOperator.Contains => scopeText.Contains(condition.Text, comparison),
                ConditionOperator.StartsWith => scopeText.StartsWith(condition.Text, comparison),
                ConditionOperator.EndsWith => scopeText.EndsWith(condition.Text, comparison),
                ConditionOperator.Equals => string.Equals(scopeText, condition.Text, comparison),
                _ => _conditionRegex?.IsMatch(scopeText) ?? false
            };
            return condition.Mode == ConditionMode.WhenMatches ? isMatched : !isMatched;
        }

        private (string Text, bool IsApplied) Transform(string text) => _trigger switch
        {
            ReplaceTrigger replaceTrigger => Replace(replaceTrigger, text),
            InsertTrigger insertTrigger => (Insert(insertTrigger, text), true),
            CaseConversionTrigger caseConversionTrigger => (ConvertCase(text, caseConversionTrigger.Mode), true),
            _ => (text, false)
        };

        private (string Text, bool IsApplied) Replace(ReplaceTrigger replaceTrigger, string text)
        {
            var contentText = FormatContent(replaceTrigger.Content);
            if (replaceTrigger.Target == ReplaceTarget.WholeName) return (contentText, true);
            if (_findRegex is null) return (text, false);

            var isMatched = false;
            var replacementCount = replaceTrigger.Occurrence == MatchOccurrence.All ? -1 : 1;

            // Regular expression text replacements support substitutions such as $1.
            var usesSubstitutions = replaceTrigger.UseRegularExpression && replaceTrigger.Content.Kind == TriggerContentKind.Text;
            var replacedText = _findRegex.Replace(text, match =>
            {
                isMatched = true;
                return usesSubstitutions ? match.Result(contentText) : contentText;
            }, replacementCount);
            return (replacedText, isMatched);
        }

        private string Insert(InsertTrigger insertTrigger, string text)
        {
            var insertIndex = insertTrigger.Position switch
            {
                InsertPosition.Beginning => 0,
                InsertPosition.End => text.Length,
                _ => Math.Clamp(insertTrigger.CharacterIndex, 0, text.Length)
            };
            return text.Insert(insertIndex, FormatContent(insertTrigger.Content));
        }

        private string FormatContent(TriggerContent content) => content.Kind == TriggerContentKind.SequenceNumber ? FormatSequenceNumber(content.StartNumber + (content.Step * _appliedCount), content.Digits) : content.Text;
    }
}
