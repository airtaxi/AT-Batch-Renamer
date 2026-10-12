using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace BatchRenamerAT.ViewModels;

// Live preview of the new names. Refresh requests are debounced and computed off the UI thread;
// a newer request cancels the pending one. After a rename, the preview shows the per-item results
// until the next refresh.
public sealed partial class RenamePreviewViewModel(RenamePlanService renamePlanService, LocalizationService localizationService) : BaseViewModel
{
    private static readonly TimeSpan s_refreshDelay = TimeSpan.FromMilliseconds(150);

    private CancellationTokenSource? _refreshCancellationTokenSource;

    public ObservableCollection<RenamePreviewItemViewModel> Items { get; } = [];

    public List<RenamePlanItem> CurrentPlan { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    public partial int ChangedCount { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanRename))]
    public partial int ConflictCount { get; private set; }

    [ObservableProperty]
    public partial string SummaryText { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsEmpty { get; private set; } = true;

    public bool CanRename => ChangedCount > 0 && ConflictCount == 0;

    public async void RequestRefresh(IReadOnlyList<RenameSource> sources, IReadOnlyList<RenameTrigger> triggers)
    {
        var cancellationToken = ResetRefreshCancellation();
        try
        {
            await Task.Delay(s_refreshDelay, cancellationToken);
            var plan = await Task.Run(() => renamePlanService.CreatePlan(sources, triggers, cancellationToken), cancellationToken);
            ApplyPlan(plan);
        }
        catch (OperationCanceledException) { }
    }

    // Computes the plan immediately, replacing any pending refresh, so a rename always runs
    // against the current files and triggers.
    public async Task<List<RenamePlanItem>> RefreshNowAsync(IReadOnlyList<RenameSource> sources, IReadOnlyList<RenameTrigger> triggers)
    {
        var cancellationToken = ResetRefreshCancellation();
        var plan = await Task.Run(() => renamePlanService.CreatePlan(sources, triggers, cancellationToken), cancellationToken);
        ApplyPlan(plan);
        return plan;
    }

    public void ShowResults(IReadOnlyList<RenameOperationResult> results)
    {
        ResetRefreshCancellation();
        CurrentPlan = [];

        var succeededCount = results.Count(result => result.IsSuccessful);
        UpdateItems([.. results.Select(result => (result.OriginalName, result.Operation.TargetName, result.IsSuccessful ? RenamePreviewStatus.Renamed : RenamePreviewStatus.Failed, result.ErrorMessage))]);
        ChangedCount = 0;
        ConflictCount = 0;
        SummaryText = localizationService.GetFormattedString("PreviewResultSummaryFormat", succeededCount, results.Count - succeededCount);
    }

    private CancellationToken ResetRefreshCancellation()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource = new CancellationTokenSource();
        return _refreshCancellationTokenSource.Token;
    }

    private void ApplyPlan(List<RenamePlanItem> plan)
    {
        CurrentPlan = plan;
        UpdateItems([.. plan.Select(planItem => (planItem.OriginalName, planItem.NewName, planItem.Status, planItem.ErrorMessage))]);

        ChangedCount = plan.Count(planItem => planItem.Status == RenamePreviewStatus.Changed);
        ConflictCount = plan.Count(planItem => IsBlockingStatus(planItem.Status));
        var unchangedCount = plan.Count(planItem => planItem.Status == RenamePreviewStatus.Unchanged);
        SummaryText = plan.Count == 0 ? "" : localizationService.GetFormattedString("PreviewSummaryFormat", ChangedCount, unchangedCount, ConflictCount);
    }

    // Reuses the existing item view models so the list does not flicker on every refresh.
    private void UpdateItems(List<(string OriginalName, string NewName, RenamePreviewStatus Status, string? DetailText)> entries)
    {
        while (Items.Count > entries.Count) Items.RemoveAt(Items.Count - 1);
        while (Items.Count < entries.Count) Items.Add(new RenamePreviewItemViewModel());

        for (var entryIndex = 0; entryIndex < entries.Count; entryIndex++)
        {
            var (originalName, newName, status, detailText) = entries[entryIndex];
            var item = Items[entryIndex];
            item.OriginalName = originalName;
            item.NewName = newName;
            item.Status = status;
            item.StatusText = GetStatusText(status);
            item.DetailText = string.IsNullOrEmpty(detailText) ? null : detailText;
        }

        IsEmpty = Items.Count == 0;
    }

    // Missing items are skipped by a rename, so they do not block it.
    private static bool IsBlockingStatus(RenamePreviewStatus status) => status is RenamePreviewStatus.Duplicate or RenamePreviewStatus.AlreadyExists or RenamePreviewStatus.InvalidCharacters or RenamePreviewStatus.EmptyName or RenamePreviewStatus.ReservedName or RenamePreviewStatus.TriggerError;

    private string GetStatusText(RenamePreviewStatus status) => localizationService.GetLocalizedString("PreviewStatus" + status);
}
