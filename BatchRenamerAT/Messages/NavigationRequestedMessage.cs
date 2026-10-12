using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;

namespace BatchRenamerAT.Messages;

// Navigation request that carries the originating host's XamlRoot, so only the owning window
// navigates its root frame.
public sealed class NavigationRequestedMessage(XamlRoot xamlRoot, Type pageType, object? parameter)
{
    public XamlRoot XamlRoot { get; } = xamlRoot;

    public Type PageType { get; } = pageType;

    public object? Parameter { get; } = parameter;

    public static void Send(XamlRoot xamlRoot, Type pageType, object? parameter) => WeakReferenceMessenger.Default.Send(new NavigationRequestedMessage(xamlRoot, pageType, parameter));
}
