using BatchRenamerAT.Helpers;
using BatchRenamerAT.Messages;
using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using BatchRenamerAT.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Windows.Storage.Pickers;
using WinUIEx;

namespace BatchRenamerAT.Views;

// Base for every app window. Fulfills its own view model's requests on the window content and
// receives loading, navigation, and close-block messages sent by the pages and controls it
// hosts; messages from other windows are ignored by comparing XamlRoot references.
public abstract class BaseWindow : WindowEx,
    IRecipient<LoadingStateRequestedMessage>,
    IRecipient<ShowLoadingMessage>,
    IRecipient<HideLoadingMessage>,
    IRecipient<NavigationRequestedMessage>,
    IRecipient<WindowCloseBlockRequestedMessage>
{
    // Serializes loading overlay sequences so concurrent requests cannot show and hide the overlay
    // in an interleaved order.
    private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

    private readonly BaseViewModel? _viewModel;

    private bool _isCloseBlocked;
    private string? _closeBlockedReason;
    private bool _isCloseBlockedNoticeOpen;

    protected readonly ApplicationThemeService _applicationThemeService = App.Services.GetRequiredService<ApplicationThemeService>();
    protected readonly LocalizationService _localizationService = App.Services.GetRequiredService<LocalizationService>();

    protected BaseWindow() : this(null) { }

    protected BaseWindow(BaseViewModel? viewModel)
    {
        _viewModel = viewModel;

        Closed += OnBaseWindowClosed;

        // Wired once here so the close-block flag alone decides whether a close is cancelled.
        AppWindow.Closing += (_, args) =>
        {
            if (!_isCloseBlocked) return;

            args.Cancel = true;
            ShowCloseBlockedNotice();
        };

        AppWindow.SetIcon("Assets/Icon.ico");

        WeakReferenceMessenger.Default.Register((IRecipient<LoadingStateRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<ShowLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<HideLoadingMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<NavigationRequestedMessage>)this);
        WeakReferenceMessenger.Default.Register((IRecipient<WindowCloseBlockRequestedMessage>)this);
    }

    public bool IsCloseBlocked => _isCloseBlocked;

    // XAML-declared lifecycle hooks: derived window roots declare Loaded="OnWindowLoaded" and
    // windows declare Closed="OnWindowClosed". The theme is registered here because the window
    // content only exists after the derived InitializeComponent.
    protected virtual void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        if (Content is FrameworkElement contentElement) _applicationThemeService.RegisterThemeTarget(contentElement);
        SubscribeViewModelEvents();
    }

    protected virtual void OnWindowClosed(object sender, WindowEventArgs args) => UnsubscribeViewModelEvents();

    // Derived windows override this and call base to add the events specific to their view model.
    protected virtual void SubscribeViewModelEvents()
    {
        if (_viewModel is null) return;

        _viewModel.MessageDialogRequested += OnMessageDialogRequested;
        _viewModel.FilePickRequested += OnFilePickRequested;
        _viewModel.FilesPickRequested += OnFilesPickRequested;
        _viewModel.SaveFileRequested += OnSaveFileRequested;
        _viewModel.FolderPickRequested += OnFolderPickRequested;
        _viewModel.LoadingStateRequested += OnLoadingStateRequested;
        _viewModel.ShowLoadingRequested += OnShowLoadingRequested;
        _viewModel.HideLoadingRequested += OnHideLoadingRequested;
        _viewModel.NavigationRequested += OnNavigationRequested;
        _viewModel.WindowCloseBlockRequested += OnWindowCloseBlockRequested;
    }

    protected virtual void UnsubscribeViewModelEvents()
    {
        if (_viewModel is null) return;

        _viewModel.MessageDialogRequested -= OnMessageDialogRequested;
        _viewModel.FilePickRequested -= OnFilePickRequested;
        _viewModel.FilesPickRequested -= OnFilesPickRequested;
        _viewModel.SaveFileRequested -= OnSaveFileRequested;
        _viewModel.FolderPickRequested -= OnFolderPickRequested;
        _viewModel.LoadingStateRequested -= OnLoadingStateRequested;
        _viewModel.ShowLoadingRequested -= OnShowLoadingRequested;
        _viewModel.HideLoadingRequested -= OnHideLoadingRequested;
        _viewModel.NavigationRequested -= OnNavigationRequested;
        _viewModel.WindowCloseBlockRequested -= OnWindowCloseBlockRequested;
    }

    private void OnMessageDialogRequested(object? sender, MessageDialogRequestedEventArgs args) => args.ResultTask = Content.ShowMessageDialogAsync(args.Parameters);

    private void OnFilePickRequested(object? sender, PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult> args) => args.ResultTask = Content.PickFileAsync(args.Parameters);

    private void OnFilesPickRequested(object? sender, PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>> args) => args.ResultTask = Content.PickFilesAsync(args.Parameters);

    private void OnSaveFileRequested(object? sender, PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult> args) => args.ResultTask = Content.SaveFileAsync(args.Parameters);

    private void OnFolderPickRequested(object? sender, PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult> args) => args.ResultTask = Content.PickFolderAsync(args.Parameters);

    private void OnLoadingStateRequested(object? sender, LoadingStateRequestedEventArgs args) => LoadingStateRequestedMessage.Send(Content.XamlRoot, args);

    private void OnShowLoadingRequested(object? sender, ShowLoadingRequestedEventArgs args) => ShowLoadingMessage.Send(Content.XamlRoot, args);

    private void OnHideLoadingRequested(object? sender, HideLoadingRequestedEventArgs args) => HideLoadingMessage.Send(Content.XamlRoot);

    private void OnNavigationRequested(object? sender, NavigationRequestedEventArgs args) => NavigationRequestedMessage.Send(Content.XamlRoot, args.PageType, args.Parameter);

    private void OnWindowCloseBlockRequested(object? sender, WindowCloseBlockRequestedEventArgs args) => SetCloseBlocked(args.IsCloseBlocked, args.Reason);

    public void Receive(LoadingStateRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        _ = RunLoadingAsync(message);
    }

    public void Receive(ShowLoadingMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        ShowLoading(message.LoadingMessage);
    }

    public void Receive(HideLoadingMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        HideLoading();
    }

    public void Receive(NavigationRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        Navigate(message.PageType, message.Parameter);
    }

    public void Receive(WindowCloseBlockRequestedMessage message)
    {
        if (Content.XamlRoot != message.XamlRoot) return;

        SetCloseBlocked(message.IsCloseBlocked, message.Reason);
    }

    // While blocked, every close attempt (the title bar close button, Alt+F4, the system menu, or a
    // programmatic Close) is cancelled and the notice with the optional reason is shown.
    public void SetCloseBlocked(bool isCloseBlocked, string? reason = null)
    {
        _isCloseBlocked = isCloseBlocked;
        _closeBlockedReason = reason;
    }

    private void ShowCloseBlockedNotice()
    {
        if (_isCloseBlockedNoticeOpen) return;

        _isCloseBlockedNoticeOpen = true;
        _ = ShowCloseBlockedNoticeAsync();
    }

    private async Task ShowCloseBlockedNoticeAsync()
    {
        try { await Content.ShowMessageDialogAsync(new MessageDialogParameters(_localizationService.GetLocalizedString("CloseBlockedTitle"), _closeBlockedReason ?? _localizationService.GetLocalizedString("CloseBlockedDefaultReason"))); }
        finally { _isCloseBlockedNoticeOpen = false; }
    }

    // Detaches the messenger registrations on close so closed windows are not kept alive.
    private void OnBaseWindowClosed(object? _, WindowEventArgs __)
    {
        WeakReferenceMessenger.Default.Unregister<LoadingStateRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<ShowLoadingMessage>(this);
        WeakReferenceMessenger.Default.Unregister<HideLoadingMessage>(this);
        WeakReferenceMessenger.Default.Unregister<NavigationRequestedMessage>(this);
        WeakReferenceMessenger.Default.Unregister<WindowCloseBlockRequestedMessage>(this);
    }

    private async Task RunLoadingAsync(LoadingStateRequestedMessage message)
    {
        await _loadingSemaphore.WaitAsync();
        try
        {
            ShowLoading(message.LoadingMessage);
            await message.Action();
            message.Complete();
        }
        catch (Exception exception) { message.Fail(exception); }
        finally
        {
            HideLoading();
            _loadingSemaphore.Release();
        }
    }

    protected abstract void Navigate(Type pageType, object? parameter);

    protected abstract void ShowLoading(string? message = null);

    protected abstract void HideLoading();
}
