using BatchRenamerAT.Models;
using BatchRenamerAT.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace BatchRenamerAT.Helpers;

// ContentDialog helpers bound to the live XamlRoot of the calling element. Every dialog follows
// the app theme, and an open dialog is closed first because a XamlRoot hosts one dialog at a time.
public static class DialogHelper
{
    private static readonly Lazy<ApplicationThemeService> s_applicationThemeServiceLazy = new(() => App.Services.GetRequiredService<ApplicationThemeService>());
    private static readonly Lazy<LocalizationService> s_localizationServiceLazy = new(() => App.Services.GetRequiredService<LocalizationService>());

    private static ApplicationThemeService ApplicationThemeService => s_applicationThemeServiceLazy.Value;

    private static LocalizationService LocalizationService => s_localizationServiceLazy.Value;

    // Shows a caller-built ContentDialog: assigns the XamlRoot, applies the app theme, hides any
    // open dialogs, and awaits the user's dismissal.
    public static async Task<ContentDialogResult> ShowContentDialogAsync(this UIElement element, ContentDialog dialog)
    {
        HideOpenContentDialogs(element);

        dialog.XamlRoot = element.XamlRoot;
        ApplicationThemeService.RegisterThemeTarget(dialog);

        return await dialog.ShowAsync();
    }

    public static async Task<ContentDialogResult> ShowMessageDialogAsync(this UIElement element, MessageDialogParameters parameters)
    {
        var dialog = new ContentDialog();
        dialog.Title = parameters.Title;
        dialog.Content = new TextBlock { Text = parameters.Description, TextWrapping = TextWrapping.Wrap };
        dialog.PrimaryButtonText = parameters.PrimaryButtonText ?? LocalizationService.GetLocalizedString("DialogOkButtonText");
        dialog.DefaultButton = ContentDialogButton.Primary;
        if (!string.IsNullOrEmpty(parameters.SecondaryButtonText)) dialog.SecondaryButtonText = parameters.SecondaryButtonText;
        if (!string.IsNullOrEmpty(parameters.CancelButtonText)) dialog.CloseButtonText = parameters.CancelButtonText;

        return await element.ShowContentDialogAsync(dialog);
    }

    private static void HideOpenContentDialogs(UIElement element)
    {
        var contentDialogs = VisualTreeHelper.GetOpenPopupsForXamlRoot(element.XamlRoot).Select(popup => popup.Child).OfType<ContentDialog>().ToList();
        foreach (var contentDialog in contentDialogs) contentDialog.Hide();
    }
}
