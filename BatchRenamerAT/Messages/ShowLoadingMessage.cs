using BatchRenamerAT.Models;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace BatchRenamerAT.Messages;

// Show request that carries the originating host's XamlRoot, so only the owning window reacts to it.
public sealed class ShowLoadingMessage(XamlRoot xamlRoot, string? loadingMessage)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public string? LoadingMessage { get; } = loadingMessage;

    public static void Send(XamlRoot xamlRoot, ShowLoadingRequestedEventArgs args) => WeakReferenceMessenger.Default.Send(new ShowLoadingMessage(xamlRoot, args.LoadingMessage));
}
