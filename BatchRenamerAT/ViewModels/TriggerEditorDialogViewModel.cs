using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace BatchRenamerAT.ViewModels;

// Editable state of the trigger editor dialog. ComboBox selections are stored as indices that
// match the enum values, and the derived visibility, validation, and example state is refreshed
// whenever an input changes.
public sealed partial class TriggerEditorDialogViewModel : ObservableObject
{
    private const string DefaultSampleName = "Photo 2026-10-12 001.jpg";

    private readonly LocalizationService _localizationService;
    private readonly RenameSource? _sampleSource;
    private readonly bool _isTriggerEnabled;
    private readonly bool _isInitialized;

    private bool _isUpdatingDerivedState;

    public TriggerEditorDialogViewModel(LocalizationService localizationService, RenameTrigger? trigger, RenameSource? sampleSource)
    {
        _localizationService = localizationService;
        _sampleSource = sampleSource;
        _isTriggerEnabled = trigger?.IsEnabled ?? true;

        var isEditing = trigger is not null;
        Title = localizationService.GetLocalizedString(isEditing ? "TriggerEditorEditTitle" : "TriggerEditorAddTitle");
        PrimaryButtonText = localizationService.GetLocalizedString(isEditing ? "TriggerEditorSaveButtonText" : "TriggerEditorAddButtonText");
        if (trigger is not null) LoadTrigger(trigger);

        _isInitialized = true;
        UpdateDerivedState();
    }

    public string Title { get; }

    public string PrimaryButtonText { get; }

    [ObservableProperty]
    public partial int ActionIndex { get; set; }

    [ObservableProperty]
    public partial int ScopeIndex { get; set; }

    [ObservableProperty]
    public partial int ReplaceTargetIndex { get; set; } = (int)ReplaceTarget.SpecificText;

    [ObservableProperty]
    public partial string FindText { get; set; } = "";

    [ObservableProperty]
    public partial bool UseRegularExpression { get; set; }

    [ObservableProperty]
    public partial bool IsCaseSensitive { get; set; }

    [ObservableProperty]
    public partial int OccurrenceIndex { get; set; }

    [ObservableProperty]
    public partial int ContentKindIndex { get; set; }

    [ObservableProperty]
    public partial string ContentText { get; set; } = "";

    [ObservableProperty]
    public partial double Digits { get; set; } = 1;

    [ObservableProperty]
    public partial double StartNumber { get; set; } = 1;

    [ObservableProperty]
    public partial double Step { get; set; } = 1;

    [ObservableProperty]
    public partial int InsertPositionIndex { get; set; } = (int)InsertPosition.End;

    [ObservableProperty]
    public partial double CharacterIndex { get; set; }

    [ObservableProperty]
    public partial int CaseConversionIndex { get; set; }

    [ObservableProperty]
    public partial bool IsConditionEnabled { get; set; }

    [ObservableProperty]
    public partial int ConditionScopeIndex { get; set; }

    [ObservableProperty]
    public partial int ConditionOperatorIndex { get; set; }

    [ObservableProperty]
    public partial string ConditionText { get; set; } = "";

    [ObservableProperty]
    public partial bool IsConditionCaseSensitive { get; set; }

    [ObservableProperty]
    public partial int ConditionModeIndex { get; set; }

    [ObservableProperty]
    public partial bool IsReplaceAction { get; private set; }

    [ObservableProperty]
    public partial bool IsInsertAction { get; private set; }

    [ObservableProperty]
    public partial bool IsCaseConversionAction { get; private set; }

    [ObservableProperty]
    public partial bool IsSpecificTextTarget { get; private set; }

    [ObservableProperty]
    public partial bool HasContent { get; private set; }

    [ObservableProperty]
    public partial bool IsTextContent { get; private set; }

    [ObservableProperty]
    public partial bool IsSequenceNumberContent { get; private set; }

    [ObservableProperty]
    public partial bool IsAfterCharacterPosition { get; private set; }

    // Regular expression text replacements accept substitutions such as $1.
    [ObservableProperty]
    public partial bool IsSubstitutionHintVisible { get; private set; }

    [ObservableProperty]
    public partial string ValidationMessage { get; private set; } = "";

    [ObservableProperty]
    public partial bool HasValidationMessage { get; private set; }

    [ObservableProperty]
    public partial bool IsValid { get; private set; }

    [ObservableProperty]
    public partial string SampleText { get; private set; } = "";

    public RenameTrigger CreateTrigger()
    {
        var scope = ToEnum(ScopeIndex, NameScope.FileName);
        var condition = IsConditionEnabled ? CreateCondition() : null;
        var content = new TriggerContent
        {
            Kind = ToEnum(ContentKindIndex, TriggerContentKind.Text),
            Text = ContentText,
            Digits = (int)Math.Clamp(ToWholeNumber(Digits, 1), 1, RenameTriggerProcessor.MaximumDigits),
            StartNumber = ToWholeNumber(StartNumber, 1),
            Step = ToWholeNumber(Step, 1)
        };

        return ToEnum(ActionIndex, TriggerActionType.Replace) switch
        {
            TriggerActionType.Replace => new ReplaceTrigger
            {
                IsEnabled = _isTriggerEnabled,
                Scope = scope,
                Condition = condition,
                Target = ToEnum(ReplaceTargetIndex, ReplaceTarget.SpecificText),
                FindText = FindText,
                UseRegularExpression = UseRegularExpression,
                IsCaseSensitive = IsCaseSensitive,
                Occurrence = ToEnum(OccurrenceIndex, MatchOccurrence.All),
                Content = content
            },
            TriggerActionType.Insert => new InsertTrigger
            {
                IsEnabled = _isTriggerEnabled,
                Scope = scope,
                Condition = condition,
                Position = ToEnum(InsertPositionIndex, InsertPosition.End),
                CharacterIndex = (int)Math.Max(ToWholeNumber(CharacterIndex, 0), 0),
                Content = content
            },
            _ => new CaseConversionTrigger
            {
                IsEnabled = _isTriggerEnabled,
                Scope = scope,
                Condition = condition,
                Mode = ToEnum(CaseConversionIndex, CaseConversionMode.Uppercase)
            }
        };
    }

    // Any input change refreshes the derived state; changes of the derived properties themselves
    // are skipped to avoid re-entrancy.
    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);
        if (_isInitialized && !_isUpdatingDerivedState) UpdateDerivedState();
    }

    private TriggerCondition CreateCondition() => new()
    {
        Scope = ToEnum(ConditionScopeIndex, NameScope.FileName),
        Operator = ToEnum(ConditionOperatorIndex, ConditionOperator.Contains),
        Text = ConditionText,
        IsCaseSensitive = IsConditionCaseSensitive,
        Mode = ToEnum(ConditionModeIndex, ConditionMode.WhenMatches)
    };

    private void LoadTrigger(RenameTrigger trigger)
    {
        ScopeIndex = (int)trigger.Scope;
        switch (trigger)
        {
            case ReplaceTrigger replaceTrigger:
                ActionIndex = (int)TriggerActionType.Replace;
                ReplaceTargetIndex = (int)replaceTrigger.Target;
                FindText = replaceTrigger.FindText;
                UseRegularExpression = replaceTrigger.UseRegularExpression;
                IsCaseSensitive = replaceTrigger.IsCaseSensitive;
                OccurrenceIndex = (int)replaceTrigger.Occurrence;
                LoadContent(replaceTrigger.Content);
                break;
            case InsertTrigger insertTrigger:
                ActionIndex = (int)TriggerActionType.Insert;
                InsertPositionIndex = (int)insertTrigger.Position;
                CharacterIndex = insertTrigger.CharacterIndex;
                LoadContent(insertTrigger.Content);
                break;
            case CaseConversionTrigger caseConversionTrigger:
                ActionIndex = (int)TriggerActionType.ConvertCase;
                CaseConversionIndex = (int)caseConversionTrigger.Mode;
                break;
        }

        if (trigger.Condition is not { } condition) return;

        IsConditionEnabled = true;
        ConditionScopeIndex = (int)condition.Scope;
        ConditionOperatorIndex = (int)condition.Operator;
        ConditionText = condition.Text;
        IsConditionCaseSensitive = condition.IsCaseSensitive;
        ConditionModeIndex = (int)condition.Mode;
    }

    private void LoadContent(TriggerContent content)
    {
        ContentKindIndex = (int)content.Kind;
        ContentText = content.Text;
        Digits = content.Digits;
        StartNumber = content.StartNumber;
        Step = content.Step;
    }

    private void UpdateDerivedState()
    {
        _isUpdatingDerivedState = true;
        try
        {
            var action = ToEnum(ActionIndex, TriggerActionType.Replace);
            IsReplaceAction = action == TriggerActionType.Replace;
            IsInsertAction = action == TriggerActionType.Insert;
            IsCaseConversionAction = action == TriggerActionType.ConvertCase;
            IsSpecificTextTarget = IsReplaceAction && ToEnum(ReplaceTargetIndex, ReplaceTarget.SpecificText) == ReplaceTarget.SpecificText;
            HasContent = !IsCaseConversionAction;
            IsTextContent = HasContent && ToEnum(ContentKindIndex, TriggerContentKind.Text) == TriggerContentKind.Text;
            IsSequenceNumberContent = HasContent && !IsTextContent;
            IsAfterCharacterPosition = IsInsertAction && ToEnum(InsertPositionIndex, InsertPosition.End) == InsertPosition.AfterCharacter;
            IsSubstitutionHintVisible = IsSpecificTextTarget && UseRegularExpression && IsTextContent;

            ValidationMessage = Validate() ?? "";
            HasValidationMessage = ValidationMessage.Length > 0;
            IsValid = !HasValidationMessage;
            SampleText = IsValid ? CreateSampleText() : "";
        }
        finally { _isUpdatingDerivedState = false; }
    }

    private string? Validate()
    {
        if (IsSpecificTextTarget)
        {
            if (FindText.Length == 0) return _localizationService.GetLocalizedString("ValidationFindTextRequired");
            if (UseRegularExpression && GetRegularExpressionError(FindText) is { } findTextError) return _localizationService.GetFormattedString("ValidationInvalidRegularExpressionFormat", findTextError);
        }

        if (IsInsertAction && IsTextContent && ContentText.Length == 0) return _localizationService.GetLocalizedString("ValidationInsertTextRequired");

        if (IsConditionEnabled)
        {
            if (ConditionText.Length == 0) return _localizationService.GetLocalizedString("ValidationConditionTextRequired");
            if (ToEnum(ConditionOperatorIndex, ConditionOperator.Contains) == ConditionOperator.MatchesRegularExpression && GetRegularExpressionError(ConditionText) is { } conditionTextError) return _localizationService.GetFormattedString("ValidationInvalidRegularExpressionFormat", conditionTextError);
        }

        return null;
    }

    private string CreateSampleText()
    {
        var sampleName = _sampleSource is null ? DefaultSampleName : Path.GetFileName(_sampleSource.Path);
        var isDirectory = _sampleSource?.IsDirectory ?? false;
        try
        {
            var renameTriggerProcessor = new RenameTriggerProcessor([CreateTrigger() with { IsEnabled = true }]);
            return $"{sampleName} → {renameTriggerProcessor.Apply(sampleName, isDirectory)}";
        }
        catch (Exception exception) when (exception is ArgumentException or RegexMatchTimeoutException) { return ""; }
    }

    private static string? GetRegularExpressionError(string pattern)
    {
        try
        {
            _ = new Regex(pattern);
            return null;
        }
        catch (ArgumentException exception) { return exception.Message; }
    }

    private static TEnum ToEnum<TEnum>(int index, TEnum fallback) where TEnum : struct, Enum
    {
        var value = (TEnum)Enum.ToObject(typeof(TEnum), index);
        return Enum.IsDefined(value) ? value : fallback;
    }

    // NumberBox reports NaN when it is cleared.
    private static long ToWholeNumber(double value, long fallback) => double.IsNaN(value) || double.IsInfinity(value) ? fallback : (long)Math.Round(value);
}
