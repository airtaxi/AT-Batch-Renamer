using BatchRenamerAT.Models;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace BatchRenamerAT.Messages;

// Loading request that carries the originating host's XamlRoot, so only the owning window runs
// the action behind its overlay; the completion reports the action's outcome back to the sender.
public sealed class LoadingStateRequestedMessage(XamlRoot xamlRoot, string? loadingMessage, Func<Task> action)
{
    private readonly TaskCompletionSource _taskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public XamlRoot XamlRoot { get; } = xamlRoot;

    public string? LoadingMessage { get; } = loadingMessage;

    public Func<Task> Action { get; } = action;

    public Task Completion => _taskCompletionSource.Task;

    public void Complete() => _taskCompletionSource.TrySetResult();

    public void Fail(Exception exception) => _taskCompletionSource.TrySetException(exception);

    public static void Send(XamlRoot xamlRoot, LoadingStateRequestedEventArgs args)
    {
        var message = new LoadingStateRequestedMessage(xamlRoot, args.LoadingMessage, args.Action);
        args.ResultTask = message.Completion;
        WeakReferenceMessenger.Default.Send(message);
    }
}
