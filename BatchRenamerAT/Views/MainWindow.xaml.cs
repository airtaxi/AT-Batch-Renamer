using BatchRenamerAT.Helpers;
using BatchRenamerAT.Pages;
using BatchRenamerAT.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using WinUIEx;

namespace BatchRenamerAT.Views;

public sealed partial class MainWindow : BaseWindow
{
    private readonly MainWindowViewModel _viewModel;

    public MainWindow() : this(App.Services.GetRequiredService<MainWindowViewModel>()) { }

    private MainWindow(MainWindowViewModel viewModel) : base(viewModel)
    {
        _viewModel = viewModel;

        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        this.CenterOnScreen();

        // Paths passed by the Explorer context menu or on the command line are added once the page loads.
        AppFrame.Navigate(typeof(MainPage), LaunchArgumentsHelper.GetLaunchPaths());
    }

    public MainWindowViewModel ViewModel => _viewModel;

    protected override void Navigate(Type pageType, object? parameter) => AppFrame.Navigate(pageType, parameter);

    protected override void ShowLoading(string? message = null)
    {
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Visible, message);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Visible, message));
    }

    protected override void HideLoading()
    {
        if (DispatcherQueue.HasThreadAccess) SetLoadingState(Visibility.Collapsed, null);
        else DispatcherQueue.TryEnqueue(() => SetLoadingState(Visibility.Collapsed, null));
    }

    private void SetLoadingState(Visibility visibility, string? message)
    {
        LoadingGrid.Visibility = visibility;
        AppFrame.IsEnabled = visibility == Visibility.Collapsed;
        LoadingTextBlock.Text = message ?? "";
        LoadingTextBlock.Visibility = string.IsNullOrWhiteSpace(message) ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnSettingsButtonClicked(object sender, RoutedEventArgs e) => new SettingsWindow(App.Services.GetRequiredService<SettingsWindowViewModel>()).ActivateModal(this);
}
