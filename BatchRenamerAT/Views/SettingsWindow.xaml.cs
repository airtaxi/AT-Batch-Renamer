using BatchRenamerAT.ViewModels;
using Microsoft.UI.Xaml.Input;
using WinUIEx;

namespace BatchRenamerAT.Views;

public sealed partial class SettingsWindow : BaseWindow
{
    private readonly SettingsWindowViewModel _viewModel;

    public SettingsWindow(SettingsWindowViewModel viewModel) : base(viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.CenterOnScreen();
    }

    public SettingsWindowViewModel ViewModel => _viewModel;

    // The settings window has no frame and every setting applies instantly, so navigation and the
    // loading overlay are not used.
    protected override void Navigate(Type pageType, object? parameter) { }

    protected override void ShowLoading(string? message = null) { }

    protected override void HideLoading() { }

    private void OnCloseKeyboardAcceleratorInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        args.Handled = true;
        Close();
    }
}
