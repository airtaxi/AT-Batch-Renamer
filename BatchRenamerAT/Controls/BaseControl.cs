using BatchRenamerAT.Helpers;
using BatchRenamerAT.Messages;
using BatchRenamerAT.Models;
using BatchRenamerAT.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace BatchRenamerAT.Controls;

// Base for user controls that host their own view model. Fulfills the view model's dialog and
// picker requests with the control-bound helpers and forwards window-level requests to the
// owning window.
public partial class BaseControl : UserControl
{
    public virtual BaseViewModel? ViewModel { get; }

    // XAML-declared lifecycle hooks: derived control roots declare Loaded="OnControlLoaded"
    // Unloaded="OnControlUnloaded", so the wiring follows every attach and detach.
    protected virtual void OnControlLoaded(object sender, RoutedEventArgs e) => SubscribeViewModelEvents();

    protected virtual void OnControlUnloaded(object sender, RoutedEventArgs e) => UnsubscribeViewModelEvents();

    // Derived controls override this and call base to add the events specific to their view model.
    protected virtual void SubscribeViewModelEvents()
    {
        if (ViewModel is null) return;

        ViewModel.MessageDialogRequested += OnMessageDialogRequested;
        ViewModel.FilePickRequested += OnFilePickRequested;
        ViewModel.FilesPickRequested += OnFilesPickRequested;
        ViewModel.SaveFileRequested += OnSaveFileRequested;
        ViewModel.FolderPickRequested += OnFolderPickRequested;
        ViewModel.LoadingStateRequested += OnLoadingStateRequested;
        ViewModel.ShowLoadingRequested += OnShowLoadingRequested;
        ViewModel.HideLoadingRequested += OnHideLoadingRequested;
        ViewModel.NavigationRequested += OnNavigationRequested;
        ViewModel.WindowCloseBlockRequested += OnWindowCloseBlockRequested;
    }

    protected virtual void UnsubscribeViewModelEvents()
    {
        if (ViewModel is null) return;

        ViewModel.MessageDialogRequested -= OnMessageDialogRequested;
        ViewModel.FilePickRequested -= OnFilePickRequested;
        ViewModel.FilesPickRequested -= OnFilesPickRequested;
        ViewModel.SaveFileRequested -= OnSaveFileRequested;
        ViewModel.FolderPickRequested -= OnFolderPickRequested;
        ViewModel.LoadingStateRequested -= OnLoadingStateRequested;
        ViewModel.ShowLoadingRequested -= OnShowLoadingRequested;
        ViewModel.HideLoadingRequested -= OnHideLoadingRequested;
        ViewModel.NavigationRequested -= OnNavigationRequested;
        ViewModel.WindowCloseBlockRequested -= OnWindowCloseBlockRequested;
    }

    private void OnMessageDialogRequested(object? sender, MessageDialogRequestedEventArgs args) => args.ResultTask = this.ShowMessageDialogAsync(args.Parameters);

    private void OnFilePickRequested(object? sender, PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult> args) => args.ResultTask = this.PickFileAsync(args.Parameters);

    private void OnFilesPickRequested(object? sender, PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>> args) => args.ResultTask = this.PickFilesAsync(args.Parameters);

    private void OnSaveFileRequested(object? sender, PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult> args) => args.ResultTask = this.SaveFileAsync(args.Parameters);

    private void OnFolderPickRequested(object? sender, PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult> args) => args.ResultTask = this.PickFolderAsync(args.Parameters);

    private void OnLoadingStateRequested(object? sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(XamlRoot, args);

    private void OnShowLoadingRequested(object? sender, ShowLoadingRequestedEventArgs args) => ShowLoadingMessage.Send(XamlRoot, args);

    private void OnHideLoadingRequested(object? sender, HideLoadingRequestedEventArgs args) => HideLoadingMessage.Send(XamlRoot);

    private void OnNavigationRequested(object? sender, NavigationRequestedEventArgs args) => NavigationRequestedMessage.Send(XamlRoot, args.PageType, args.Parameter);

    private void OnWindowCloseBlockRequested(object? sender, WindowCloseBlockRequestedEventArgs args) => WindowCloseBlockRequestedMessage.Send(XamlRoot, args.IsCloseBlocked, args.Reason);
}
