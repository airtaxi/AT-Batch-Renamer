using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace BatchRenamerAT.Messages;

// Close-block request that carries the originating host's XamlRoot, so only the owning window
// applies it, plus the optional reason shown when a close is attempted while the block is on.
public sealed class WindowCloseBlockRequestedMessage(XamlRoot xamlRoot, bool isCloseBlocked, string? reason = null)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public bool IsCloseBlocked { get; } = isCloseBlocked;

    public string? Reason { get; } = reason;

    public static void Send(XamlRoot xamlRoot, bool isCloseBlocked, string? reason = null) => WeakReferenceMessenger.Default.Send(new WindowCloseBlockRequestedMessage(xamlRoot, isCloseBlocked, reason));
}
