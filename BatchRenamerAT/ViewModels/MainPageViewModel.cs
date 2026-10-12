using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;

namespace BatchRenamerAT.ViewModels;

// Coordinates the file list, the trigger list, and the preview, and runs renames and undo.
public sealed partial class MainPageViewModel : BaseViewModel
{
    private readonly FileRenameService _fileRenameService;
    private readonly LocalizationService _localizationService;

    // Reverse operations of the last rename, used by undo.
    private List<RenameOperation> _undoOperations = [];

    public MainPageViewModel(FileListViewModel fileListViewModel, TriggerListViewModel triggerListViewModel, RenamePreviewViewModel renamePreviewViewModel, FileRenameService fileRenameService, LocalizationService localizationService)
    {
        FileList = fileListViewModel;
        TriggerList = triggerListViewModel;
        Preview = renamePreviewViewModel;
        _fileRenameService = fileRenameService;
        _localizationService = localizationService;

        FileList.FilesChanged += OnFileListFilesChanged;
        TriggerList.TriggersChanged += OnTriggerListTriggersChanged;
        TriggerList.SampleSourceProvider = FileList.GetFirstSource;
        Preview.PropertyChanged += OnPreviewPropertyChanged;

        UpdateRenameButtonText();
    }

    public FileListViewModel FileList { get; }

    public TriggerListViewModel TriggerList { get; }

    public RenamePreviewViewModel Preview { get; }

    [ObservableProperty]
    public partial string RenameButtonText { get; private set; } = "";

    [ObservableProperty]
    public partial bool IsResultInfoBarOpen { get; set; }

    [ObservableProperty]
    public partial string ResultInfoBarTitle { get; private set; } = "";

    [ObservableProperty]
    public partial string ResultInfoBarMessage { get; private set; } = "";

    [ObservableProperty]
    public partial InfoBarSeverity ResultInfoBarSeverity { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UndoCommand))]
    public partial bool CanUndo { get; private set; }

    private bool CanRename => Preview.CanRename;

    public async Task AddLaunchPathsAsync(IReadOnlyList<string> paths)
    {
        if (paths.Count > 0)
        {
            await FileList.AddPathsAsync(paths);
        }
    }

    [RelayCommand(CanExecute = nameof(CanRename))]
    private async Task RenameAsync()
    {
        var plan = await Preview.RefreshNowAsync(FileList.GetSources(), TriggerList.GetTriggers());
        if (!Preview.CanRename) return;

        List<RenameOperation> operations = [.. plan.Where(planItem => planItem.Status == RenamePreviewStatus.Changed).Select(planItem => new RenameOperation(planItem.Source.Path, planItem.NewName, planItem.Source.IsDirectory))];
        var results = await RunOperationsAsync(operations, _localizationService.GetLocalizedString("RenamingMessage"));
        Preview.ShowResults(results);

        _undoOperations = [.. results.Where(result => result.IsSuccessful).Select(result => new RenameOperation(result.CurrentPath, result.OriginalName, result.Operation.IsDirectory))];
        CanUndo = _undoOperations.Count > 0;
        ShowResultInfoBar(results, "RenameResultTitle");
    }

    [RelayCommand(CanExecute = nameof(CanUndo))]
    private async Task UndoAsync()
    {
        var operations = _undoOperations;
        _undoOperations = [];
        CanUndo = false;

        var results = await RunOperationsAsync(operations, _localizationService.GetLocalizedString("UndoingMessage"));

        // A full undo returns the preview to the plan it showed before the rename; failures keep the
        // per-item results visible so the items that could not be restored can be found.
        if (results.All(result => result.IsSuccessful)) await Preview.RefreshNowAsync(FileList.GetSources(), TriggerList.GetTriggers());
        else Preview.ShowResults(results);

        ShowResultInfoBar(results, "UndoResultTitle");
    }

    private async Task<List<RenameOperationResult>> RunOperationsAsync(List<RenameOperation> operations, string loadingMessage)
    {
        List<RenameOperationResult> results;
        SetWindowCloseBlocked(true, _localizationService.GetLocalizedString("CloseBlockedRenamingReason"));
        try { results = await ExecuteWithLoadingAsync(() => Task.Run(() => _fileRenameService.Execute(operations)), loadingMessage); }
        finally { SetWindowCloseBlocked(false); }

        FileList.ApplyRenameResults(results);
        return results;
    }

    private void ShowResultInfoBar(List<RenameOperationResult> results, string titleResourceName)
    {
        var succeededCount = results.Count(result => result.IsSuccessful);
        var failedCount = results.Count - succeededCount;

        ResultInfoBarTitle = _localizationService.GetLocalizedString(titleResourceName);
        ResultInfoBarMessage = failedCount == 0 ? _localizationService.GetFormattedString("ResultSucceededFormat", succeededCount) : _localizationService.GetFormattedString("ResultPartiallyFailedFormat", succeededCount, failedCount);
        ResultInfoBarSeverity = failedCount == 0 ? InfoBarSeverity.Success : succeededCount == 0 ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
        IsResultInfoBarOpen = true;
    }

    private void RefreshPreview() => Preview.RequestRefresh(FileList.GetSources(), TriggerList.GetTriggers());

    private void UpdateRenameButtonText() => RenameButtonText = Preview.ChangedCount > 0 ? _localizationService.GetFormattedString("RenameButtonCountFormat", Preview.ChangedCount) : _localizationService.GetLocalizedString("RenameButtonText");

    private void OnFileListFilesChanged(object? _, EventArgs __) => RefreshPreview();

    private void OnTriggerListTriggersChanged(object? _, EventArgs __) => RefreshPreview();

    private void OnPreviewPropertyChanged(object? _, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(RenamePreviewViewModel.CanRename)) return;

        RenameCommand.NotifyCanExecuteChanged();
        UpdateRenameButtonText();
    }
}
