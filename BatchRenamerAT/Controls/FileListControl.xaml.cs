using BatchRenamerAT.Dialogs;
using BatchRenamerAT.Helpers;
using BatchRenamerAT.Models;
using BatchRenamerAT.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;

namespace BatchRenamerAT.Controls;

public sealed partial class FileListControl : BaseControl
{
    public FileListControl()
    {
        ViewModel = App.Services.GetRequiredService<FileListViewModel>();

        InitializeComponent();

        // Registered with handledEventsToo because the reorder-enabled ListView handles drag events
        // itself; external file drops still need to reach these handlers.
        FileListView.AddHandler(DragOverEvent, new DragEventHandler(OnFileListViewDragOver), true);
        FileListView.AddHandler(DropEvent, new DragEventHandler(OnFileListViewDrop), true);
    }

    public override FileListViewModel ViewModel { get; }

    protected override void SubscribeViewModelEvents()
    {
        base.SubscribeViewModelEvents();

        ViewModel.FolderHandlingRequested += OnViewModelFolderHandlingRequested;
    }

    protected override void UnsubscribeViewModelEvents()
    {
        ViewModel.FolderHandlingRequested -= OnViewModelFolderHandlingRequested;

        base.UnsubscribeViewModelEvents();
    }

    private void OnViewModelFolderHandlingRequested(object? _, FolderHandlingRequestedEventArgs args) => args.ResultTask = ShowFolderHandlingDialogAsync(args.DialogViewModel);

    private async Task<bool> ShowFolderHandlingDialogAsync(FolderHandlingDialogViewModel dialogViewModel) => await this.ShowContentDialogAsync(new FolderHandlingDialog(dialogViewModel)) == ContentDialogResult.Primary;

    private void OnFileListViewDragOver(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        e.AcceptedOperation = DataPackageOperation.Copy;
        e.Handled = true;
    }

    private async void OnFileListViewDrop(object sender, DragEventArgs e)
    {
        if (!e.DataView.Contains(StandardDataFormats.StorageItems)) return;

        e.Handled = true;
        IReadOnlyList<IStorageItem> storageItems;
        var dragOperationDeferral = e.GetDeferral();
        try { storageItems = await e.DataView.GetStorageItemsAsync(); }
        finally { dragOperationDeferral.Complete(); }

        await ViewModel.AddPathsAsync(storageItems.Select(storageItem => storageItem.Path));
    }

    private void OnFileListViewSelectionChanged(object sender, SelectionChangedEventArgs e) => ViewModel.UpdateSelection(FileListView.SelectedItems.OfType<FileItemViewModel>());

    private void OnDeleteKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        if (ViewModel.RemoveSelectedCommand.CanExecute(null)) ViewModel.RemoveSelectedCommand.Execute(null);
    }
}
