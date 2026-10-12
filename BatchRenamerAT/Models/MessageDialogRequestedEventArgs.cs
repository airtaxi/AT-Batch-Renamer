using Microsoft.UI.Xaml.Controls;

namespace BatchRenamerAT.Models;

public sealed class MessageDialogRequestedEventArgs(MessageDialogParameters parameters) : EventArgs
{
    public MessageDialogParameters Parameters { get; } = parameters;

    public Task<ContentDialogResult>? ResultTask { get; set; }
}
