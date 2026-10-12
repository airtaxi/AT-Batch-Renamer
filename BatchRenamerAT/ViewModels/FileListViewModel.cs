using BatchRenamerAT.Enums;
using BatchRenamerAT.Helpers;
using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace BatchRenamerAT.ViewModels;

// Files and folders to rename. The list order drives sequence numbering, so it can be sorted
// or rearranged by dragging.
public sealed partial class FileListViewModel : BaseViewModel
{
    private readonly ApplicationSettingsService _applicationSettingsService;
    private readonly LocalizationService _localizationService;

    private List<FileItemViewModel> _selectedItems = [];

    public FileListViewModel(ApplicationSettingsService applicationSettingsService, LocalizationService localizationService)
    {
        _applicationSettingsService = applicationSettingsService;
        _localizationService = localizationService;

        Items.CollectionChanged += OnItemsCollectionChanged;
        UpdateItemState();
    }

    // Raised whenever items are added, removed, or reordered.
    public event EventHandler? FilesChanged;

    public event EventHandler<FolderHandlingRequestedEventArgs>? FolderHandlingRequested;

    public ObservableCollection<FileItemViewModel> Items { get; } = [];

    [ObservableProperty]
    public partial string ItemCountText { get; private set; } = "";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveAllCommand))]
    public partial bool HasItems { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemoveSelectedCommand))]
    public partial bool HasSelection { get; private set; }

    [ObservableProperty]
    public partial bool IsSortDescending { get; set; }

    public void UpdateSelection(IEnumerable<FileItemViewModel> selectedItems)
    {
        _selectedItems = [.. selectedItems];
        HasSelection = _selectedItems.Count > 0;
    }

    public List<RenameSource> GetSources() =>
    [.. Items.Select(item => new RenameSource(item.Path, item.IsDirectory))];

    public RenameSource? GetFirstSource() => Items.Count == 0 ? null : new RenameSource(Items[0].Path, Items[0].IsDirectory);

    // Adds dropped, picked, or launched paths. Folders are handled as the user chooses (or as
    // configured in the settings); paths already in the list are skipped.
    public async Task AddPathsAsync(IEnumerable<string> paths)
    {
        var normalizedPaths = paths.Select(NormalizePath).Where(path => path.Length > 0).ToList();
        var filePaths = normalizedPaths.Where(File.Exists).ToList();
        var directoryPaths = normalizedPaths.Where(path => Directory.Exists(path) && Path.GetFileName(path).Length > 0).ToList();

        List<FileItemViewModel> newItems = [.. filePaths.Select(path => new FileItemViewModel(path, false))];
        if (directoryPaths.Count > 0)
        {
            var folderHandlingMode = await GetFolderHandlingModeAsync(directoryPaths.Count);
            if (folderHandlingMode == FolderHandlingMode.RenameFolders) newItems.AddRange(directoryPaths.Select(path => new FileItemViewModel(path, true)));
            else if (folderHandlingMode is { } expandingMode)
            {
                var isRecursive = expandingMode == FolderHandlingMode.AddFilesRecursively;
                var folderFilePaths = await ExecuteWithLoadingAsync(() => Task.Run(() => EnumerateFolderFiles(directoryPaths, isRecursive)), _localizationService.GetLocalizedString("LoadingFolderFilesMessage"));
                newItems.AddRange(folderFilePaths.Select(path => new FileItemViewModel(path, false)));
            }
        }

        var existingPaths = new HashSet<string>(Items.Select(item => item.Path), StringComparer.OrdinalIgnoreCase);
        foreach (var newItem in newItems)
        {
            if (existingPaths.Add(newItem.Path))
            {
                Items.Add(newItem);
            }
        }
    }

    // Moves list items to the paths they have after a rename or undo. Items outside the batch
    // that live under a renamed folder follow that folder.
    public void ApplyRenameResults(IReadOnlyList<RenameOperationResult> results)
    {
        var itemsByPath = new Dictionary<string, FileItemViewModel>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in Items) itemsByPath.TryAdd(item.Path, item);

        foreach (var result in results)
        {
            if (itemsByPath.TryGetValue(result.Operation.SourcePath, out var item))
            {
                item.Path = result.CurrentPath;
            }
        }

        foreach (var result in results.Where(result => result.IsSuccessful && result.Operation.IsDirectory).OrderByDescending(result => result.Operation.SourcePath.Length))
        {
            var sourcePrefix = result.Operation.SourcePath + Path.DirectorySeparatorChar;
            foreach (var item in Items)
            {
                if (item.Path.StartsWith(sourcePrefix, StringComparison.OrdinalIgnoreCase))
                {
                    item.Path = result.CurrentPath + item.Path[result.Operation.SourcePath.Length..];
                }
            }
        }
    }

    [RelayCommand]
    private async Task AddFilesAsync()
    {
        var pickFileResults = await PickFilesAsync(new FileOpenPickerParameters(["*"]));
        if (pickFileResults is null || pickFileResults.Count == 0) return;

        await AddPathsAsync(pickFileResults.Select(pickFileResult => pickFileResult.Path));
    }

    [RelayCommand]
    private async Task AddFolderAsync()
    {
        var pickFolderResult = await PickFolderAsync(new FolderPickerParameters(PickerLocationId.ComputerFolder));
        if (pickFolderResult is null) return;

        await AddPathsAsync([pickFolderResult.Path]);
    }

    [RelayCommand(CanExecute = nameof(HasSelection))]
    private void RemoveSelected()
    {
        foreach (var selectedItem in _selectedItems.ToList())
        {
            Items.Remove(selectedItem);
        }
    }

    [RelayCommand(CanExecute = nameof(HasItems))]
    private void RemoveAll()
    {
        for (var itemIndex = Items.Count - 1; itemIndex >= 0; itemIndex--)
        {
            Items.RemoveAt(itemIndex);
        }
    }

    [RelayCommand]
    private void SortByName() => Sort(FileSortKey.Name);

    [RelayCommand]
    private void SortByDateModified() => Sort(FileSortKey.DateModified);

    [RelayCommand]
    private void SortByDateCreated() => Sort(FileSortKey.DateCreated);

    [RelayCommand]
    private void SortBySize() => Sort(FileSortKey.Size);

    [RelayCommand]
    private void SortByExtension() => Sort(FileSortKey.Extension);

    // Sorting is a one-time rearrangement, so items dragged into place afterwards stay where they are.
    private void Sort(FileSortKey sortKey)
    {
        if (Items.Count < 2) return;

        IEnumerable<FileItemViewModel> sortedItems = sortKey switch
        {
            FileSortKey.Name => Items.OrderBy(item => item.Name, NaturalStringComparer.Instance),
            FileSortKey.DateModified => Items.OrderBy(item => GetFileSystemInfo(item).LastWriteTimeUtc),
            FileSortKey.DateCreated => Items.OrderBy(item => GetFileSystemInfo(item).CreationTimeUtc),
            FileSortKey.Size => Items.OrderBy(GetFileSize),
            _ => Items.OrderBy(item => Path.GetExtension(item.Name), StringComparer.OrdinalIgnoreCase).ThenBy(item => item.Name, NaturalStringComparer.Instance)
        };
        var sortedItemList = sortedItems.ToList();
        if (IsSortDescending) sortedItemList.Reverse();

        for (var targetIndex = 0; targetIndex < sortedItemList.Count; targetIndex++)
        {
            var currentIndex = Items.IndexOf(sortedItemList[targetIndex]);
            if (currentIndex != targetIndex) Items.Move(currentIndex, targetIndex);
        }
    }

    private async Task<FolderHandlingMode?> GetFolderHandlingModeAsync(int folderCount)
    {
        var applicationSettings = _applicationSettingsService.Settings;
        if (!applicationSettings.AskFolderHandling) return applicationSettings.DefaultFolderHandlingMode;

        var dialogViewModel = new FolderHandlingDialogViewModel(_localizationService, folderCount, applicationSettings.DefaultFolderHandlingMode);
        var args = new FolderHandlingRequestedEventArgs(dialogViewModel);
        FolderHandlingRequested?.Invoke(this, args);
        if (args.ResultTask is null || !await args.ResultTask) return null;

        var folderHandlingMode = dialogViewModel.SelectedMode;
        if (dialogViewModel.IsRememberChoiceChecked)
        {
            applicationSettings.AskFolderHandling = false;
            applicationSettings.DefaultFolderHandlingMode = folderHandlingMode;
            _applicationSettingsService.SaveSettings();
        }

        return folderHandlingMode;
    }

    private void OnItemsCollectionChanged(object? _, NotifyCollectionChangedEventArgs __)
    {
        UpdateItemState();
        FilesChanged?.Invoke(this, EventArgs.Empty);
    }

    private void UpdateItemState()
    {
        HasItems = Items.Count > 0;
        ItemCountText = _localizationService.GetFormattedString("FileCountFormat", Items.Count);
    }

    private static List<string> EnumerateFolderFiles(List<string> directoryPaths, bool isRecursive)
    {
        var enumerationOptions = new EnumerationOptions { RecurseSubdirectories = isRecursive, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.Hidden | FileAttributes.System };
        return [.. directoryPaths.SelectMany(directoryPath => Directory.EnumerateFiles(directoryPath, "*", enumerationOptions).Order(NaturalStringComparer.Instance))];
    }

    private static string NormalizePath(string path)
    {
        var trimmedPath = path.Trim().Trim('"');
        if (trimmedPath.Length == 0) return "";

        try { return Path.TrimEndingDirectorySeparator(Path.GetFullPath(trimmedPath)); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { return ""; }
    }

    private static FileSystemInfo GetFileSystemInfo(FileItemViewModel item) => item.IsDirectory ? new DirectoryInfo(item.Path) : new FileInfo(item.Path);

    private static long GetFileSize(FileItemViewModel item)
    {
        if (item.IsDirectory) return 0;

        var fileInfo = new FileInfo(item.Path);
        return fileInfo.Exists ? fileInfo.Length : 0;
    }
}
