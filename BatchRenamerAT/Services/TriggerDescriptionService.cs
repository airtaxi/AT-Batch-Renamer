using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using System.Globalization;

namespace BatchRenamerAT.Services;

// Builds the localized summary shown for a trigger in the trigger list. The action and the
// condition are described separately so the list can show them on their own lines.
public sealed class TriggerDescriptionService(LocalizationService localizationService)
{
    public string DescribeAction(RenameTrigger trigger)
    {
        var scopeText = DescribeScope(trigger.Scope);
        return trigger switch
        {
            ReplaceTrigger replaceTrigger => DescribeReplace(replaceTrigger, scopeText),
            InsertTrigger insertTrigger => DescribeInsert(insertTrigger, scopeText),
            CaseConversionTrigger caseConversionTrigger => localizationService.GetFormattedString("DescriptionConvertCaseFormat", scopeText, DescribeCaseConversionMode(caseConversionTrigger.Mode)),
            _ => ""
        };
    }

    public string DescribeCondition(RenameTrigger trigger) => trigger.Condition is { } condition ? DescribeCondition(condition) : "";

    private string DescribeReplace(ReplaceTrigger replaceTrigger, string scopeText)
    {
        var contentText = DescribeContent(replaceTrigger.Content);
        if (replaceTrigger.Target == ReplaceTarget.WholeName) return localizationService.GetFormattedString("DescriptionReplaceWholeFormat", scopeText, contentText);

        var description = localizationService.GetFormattedString("DescriptionReplaceTextFormat", scopeText, Quote(replaceTrigger.FindText), contentText);
        List<string> options = [];
        if (replaceTrigger.UseRegularExpression) options.Add(localizationService.GetLocalizedString("DescriptionOptionRegularExpression"));
        if (replaceTrigger.IsCaseSensitive) options.Add(localizationService.GetLocalizedString("DescriptionOptionMatchCase"));
        if (replaceTrigger.Occurrence == MatchOccurrence.First) options.Add(localizationService.GetLocalizedString("DescriptionOptionFirstMatch"));
        else if (replaceTrigger.Occurrence == MatchOccurrence.Last) options.Add(localizationService.GetLocalizedString("DescriptionOptionLastMatch"));
        return options.Count == 0 ? description : $"{description} ({string.Join(", ", options)})";
    }

    private string DescribeInsert(InsertTrigger insertTrigger, string scopeText)
    {
        var contentText = DescribeContent(insertTrigger.Content);
        return insertTrigger.Position switch
        {
            InsertPosition.Beginning => localizationService.GetFormattedString("DescriptionInsertBeginningFormat", scopeText, contentText),
            InsertPosition.End => localizationService.GetFormattedString("DescriptionInsertEndFormat", scopeText, contentText),
            _ => localizationService.GetFormattedString("DescriptionInsertAfterCharacterFormat", scopeText, contentText, insertTrigger.CharacterIndex)
        };
    }

    private string DescribeContent(TriggerContent content)
    {
        if (content.Kind == TriggerContentKind.SequenceNumber) return localizationService.GetFormattedString("DescriptionSequenceNumberFormat", content.Digits, content.StartNumber.ToString(CultureInfo.CurrentCulture), content.Step.ToString(CultureInfo.CurrentCulture));
        return content.Text.Length == 0 ? localizationService.GetLocalizedString("DescriptionEmptyText") : Quote(content.Text);
    }

    private string DescribeCondition(TriggerCondition condition)
    {
        var scopeText = DescribeScope(condition.Scope);
        var resourceName = condition.Operator switch
        {
            ConditionOperator.Contains => "DescriptionConditionContainsFormat",
            ConditionOperator.StartsWith => "DescriptionConditionStartsWithFormat",
            ConditionOperator.EndsWith => "DescriptionConditionEndsWithFormat",
            ConditionOperator.Equals => "DescriptionConditionEqualsFormat",
            _ => "DescriptionConditionRegularExpressionFormat"
        };
        var conditionText = localizationService.GetFormattedString(resourceName, scopeText, Quote(condition.Text));
        if (condition.IsCaseSensitive) conditionText += $" ({localizationService.GetLocalizedString("DescriptionOptionMatchCase")})";
        return localizationService.GetFormattedString(condition.Mode == ConditionMode.WhenMatches ? "DescriptionConditionWhenMatchesFormat" : "DescriptionConditionWhenNotMatchesFormat", conditionText);
    }

    private string DescribeScope(NameScope scope) => scope switch
    {
        NameScope.FileName => localizationService.GetLocalizedString("DescriptionScopeFileName"),
        NameScope.Extension => localizationService.GetLocalizedString("DescriptionScopeExtension"),
        _ => localizationService.GetLocalizedString("DescriptionScopeFullName")
    };

    private string DescribeCaseConversionMode(CaseConversionMode caseConversionMode) => caseConversionMode switch
    {
        CaseConversionMode.Uppercase => localizationService.GetLocalizedString("DescriptionCaseUppercase"),
        CaseConversionMode.Lowercase => localizationService.GetLocalizedString("DescriptionCaseLowercase"),
        CaseConversionMode.TitleCase => localizationService.GetLocalizedString("DescriptionCaseTitleCase"),
        _ => localizationService.GetLocalizedString("DescriptionCaseSentenceCase")
    };

    private static string Quote(string text) => $"\"{text}\"";
}
