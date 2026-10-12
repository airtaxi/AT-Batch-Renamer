using BatchRenamerAT.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;

namespace BatchRenamerAT.ViewModels;

// Base for view models hosted by a BaseWindow, BasePage, or BaseControl. View models never touch
// XAML: they raise a request event, and the host view fulfills it by filling the ResultTask slot
// (dialogs, pickers) or by forwarding it to the owning window (loading overlay, navigation,
// close block). Without a subscribed host, requests return null and loading actions still run.
public abstract partial class BaseViewModel : ObservableObject
{
    public event EventHandler<MessageDialogRequestedEventArgs>? MessageDialogRequested;
    public event EventHandler<PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult>>? FilePickRequested;
    public event EventHandler<PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>>>? FilesPickRequested;
    public event EventHandler<PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult>>? SaveFileRequested;
    public event EventHandler<PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult>>? FolderPickRequested;
    public event EventHandler<LoadingStateRequestedEventArgs>? LoadingStateRequested;
    public event EventHandler<ShowLoadingRequestedEventArgs>? ShowLoadingRequested;
    public event EventHandler<HideLoadingRequestedEventArgs>? HideLoadingRequested;
    public event EventHandler<NavigationRequestedEventArgs>? NavigationRequested;
    public event EventHandler<WindowCloseBlockRequestedEventArgs>? WindowCloseBlockRequested;

    public async Task<ContentDialogResult?> ShowMessageDialogAsync(MessageDialogParameters parameters)
    {
        var args = new MessageDialogRequestedEventArgs(parameters);
        MessageDialogRequested?.Invoke(this, args);
        if (args.ResultTask is null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFileResult?> PickFileAsync(FileOpenPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileOpenPickerParameters, PickFileResult>(parameters);
        FilePickRequested?.Invoke(this, args);
        if (args.ResultTask is null) return null;

        return await args.ResultTask;
    }

    public async Task<IReadOnlyList<PickFileResult>?> PickFilesAsync(FileOpenPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileOpenPickerParameters, IReadOnlyList<PickFileResult>>(parameters);
        FilesPickRequested?.Invoke(this, args);
        if (args.ResultTask is null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFileResult?> SaveFileAsync(FileSavePickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FileSavePickerParameters, PickFileResult>(parameters);
        SaveFileRequested?.Invoke(this, args);
        if (args.ResultTask is null) return null;

        return await args.ResultTask;
    }

    public async Task<PickFolderResult?> PickFolderAsync(FolderPickerParameters parameters)
    {
        var args = new PickerRequestedEventArgs<FolderPickerParameters, PickFolderResult>(parameters);
        FolderPickRequested?.Invoke(this, args);
        if (args.ResultTask is null) return null;

        return await args.ResultTask;
    }

    // Runs the action behind the owning window's loading overlay. Exceptions thrown by the action
    // are rethrown to the caller.
    public async Task ExecuteWithLoadingAsync(Func<Task> action, string? loadingMessage = null)
    {
        var args = new LoadingStateRequestedEventArgs(loadingMessage, action);
        LoadingStateRequested?.Invoke(this, args);
        if (args.ResultTask is null) await action();
        else await args.ResultTask;
    }

    public async Task<T> ExecuteWithLoadingAsync<T>(Func<Task<T>> action, string? loadingMessage = null)
    {
        var result = default(T)!;

        // The request carries a Func<Task>, so the typed action is wrapped in a closure that
        // captures the result for the return value. The request is raised directly: passing the
        // closure to the non-generic overload would bind back to this overload, because the
        // assignment lambda is inferred as Func<Task<T>>.
        var args = new LoadingStateRequestedEventArgs(loadingMessage, async () => result = await action());
        LoadingStateRequested?.Invoke(this, args);
        if (args.ResultTask is null) await args.Action();
        else await args.ResultTask;

        return result;
    }

    public void ShowLoading(string? loadingMessage = null) => ShowLoadingRequested?.Invoke(this, new ShowLoadingRequestedEventArgs(loadingMessage));

    public void HideLoading() => HideLoadingRequested?.Invoke(this, new HideLoadingRequestedEventArgs());

    public void RequestNavigation(Type pageType, object? parameter = null) => NavigationRequested?.Invoke(this, new NavigationRequestedEventArgs(pageType, parameter));

    // Blocks or unblocks closing the owning window, so a long-running flow cannot be interrupted
    // by a close; the optional reason is shown when a close is attempted while the block is on.
    public void SetWindowCloseBlocked(bool isCloseBlocked, string? reason = null) => WindowCloseBlockRequested?.Invoke(this, new WindowCloseBlockRequestedEventArgs(isCloseBlocked, reason));
}
