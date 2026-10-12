using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Globalization;

namespace BatchRenamerAT.ViewModels;

public sealed partial class TriggerItemViewModel : ObservableObject
{
    private readonly TriggerDescriptionService _triggerDescriptionService;

    public TriggerItemViewModel(RenameTrigger trigger, TriggerDescriptionService triggerDescriptionService)
    {
        _triggerDescriptionService = triggerDescriptionService;
        Trigger = trigger;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Description))]
    public partial RenameTrigger Trigger { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(DescriptionOpacity))]
    public partial bool IsEnabled { get; set; }

    // One-based position shown in the list.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NumberText))]
    public partial int Number { get; set; }

    public string NumberText => Number.ToString(CultureInfo.CurrentCulture);

    public string Description => _triggerDescriptionService.Describe(Trigger);

    public double DescriptionOpacity => IsEnabled ? 1 : 0.5;

    partial void OnTriggerChanged(RenameTrigger value) => IsEnabled = value.IsEnabled;

    partial void OnIsEnabledChanged(bool value)
    {
        if (Trigger.IsEnabled != value)
        {
            Trigger = Trigger with
            {
                IsEnabled = value
            };
        }
    }
}
