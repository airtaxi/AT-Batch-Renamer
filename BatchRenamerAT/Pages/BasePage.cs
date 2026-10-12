using BatchRenamerAT.Helpers;
using BatchRenamerAT.Messages;
using BatchRenamerAT.Models;
using BatchRenamerAT.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage.Pickers;

namespace BatchRenamerAT.Pages;

// Base for navigable pages. Fulfills the page view model's dialog and picker requests with the
// page-bound helpers and forwards window-level requests to the owning window.
public partial class BasePage : Page
{
    // The view model instance the shared handlers are attached to, so navigation detaches the
    // same instance it attached.
    private BaseViewModel? _subscribedViewModel;

    private bool _isLoadedOnce;

    protected virtual BaseViewModel? ViewModel { get; }

    // XAML-declared load hook: derived page roots declare Loaded="OnPageLoaded". The first load
    // runs OnFirstPageLoad; later loads of a cached page run OnPageReloaded.
    protected void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (_isLoadedOnce)
        {
            OnPageReloaded();
            return;
        }

        _isLoadedOnce = true;
        OnFirstPageLoad();
    }

    // Runs once per page instance after the XamlRoot is available; start XamlRoot-dependent work
    // such as dialogs and loading here, never in OnNavigatedTo.
    protected virtual void OnFirstPageLoad() { }

    protected virtual void OnPageReloaded() { }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        SubscribeViewModelEvents();
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Leaving the page aborts any flow it guarded, so its close block must not outlive the page.
        if (XamlRoot is not null) WindowCloseBlockRequestedMessage.Send(XamlRoot, false);

        UnsubscribeViewModelEvents();
        base.OnNavigatedFrom(e);
    }

    // Derived pages override this and call base to add the events specific to their view model.
    protected virtual void SubscribeViewModelEvents()
    {
        if (ViewModel is null) return;

        _subscribedViewModel = ViewModel;

        _subscribedViewModel.MessageDialogRequested += OnMessageDialogRequested;
        _subscribedViewModel.FilePickRequested += OnFilePickRequested;
        _subscribedViewModel.FilesPickRequested += OnFilesPickRequested;
        _subscribedViewModel.SaveFileRequested += OnSaveFileRequested;
        _subscribedViewModel.FolderPickRequested += OnFolderPickRequested;
        _subscribedViewModel.LoadingStateRequested += OnLoadingStateRequested;
        _subscribedViewModel.ShowLoadingRequested += OnShowLoadingRequested;
        _subscribedViewModel.HideLoadingRequested += OnHideLoadingRequested;
        _subscribedViewModel.NavigationRequested += OnNavigationRequested;
        _subscribedViewModel.WindowCloseBlockRequested += OnWindowCloseBlockRequested;
    }

    protected virtual void UnsubscribeViewModelEvents()
    {
        if (_subscribedViewModel is null) return;

        _subscribedViewModel.MessageDialogRequested -= OnMessageDialogRequested;
        _subscribedViewModel.FilePickRequested -= OnFilePickRequested;
        _subscribedViewModel.FilesPickRequested -= OnFilesPickRequested;
        _subscribedViewModel.SaveFileRequested -= OnSaveFileRequested;
        _subscribedViewModel.FolderPickRequested -= OnFolderPickRequested;
        _subscribedViewModel.LoadingStateRequested -= OnLoadingStateRequested;
        _subscribedViewModel.ShowLoadingRequested -= OnShowLoadingRequested;
        _subscribedViewModel.HideLoadingRequested -= OnHideLoadingRequested;
        _subscribedViewModel.NavigationRequested -= OnNavigationRequested;
        _subscribedViewModel.WindowCloseBlockRequested -= OnWindowCloseBlockRequested;

        _subscribedViewModel = null;
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
