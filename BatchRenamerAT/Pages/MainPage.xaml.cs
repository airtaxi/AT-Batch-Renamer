using BatchRenamerAT.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml.Navigation;

namespace BatchRenamerAT.Pages;

public sealed partial class MainPage : BasePage
{
    private IReadOnlyList<string> _launchPaths = [];

    public MainPage()
    {
        ViewModel = App.Services.GetRequiredService<MainPageViewModel>();

        InitializeComponent();
    }

    protected override MainPageViewModel ViewModel { get; }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);

        if (e.Parameter is string[] launchPaths) _launchPaths = launchPaths;
    }

    // Queued at low priority so the panel controls have subscribed to their view models and can
    // host the folder handling dialog before the launch paths are added.
    protected override void OnFirstPageLoad() => DispatcherQueue.TryEnqueue(DispatcherQueuePriority.Low, async () => await ViewModel.AddLaunchPathsAsync(_launchPaths));
}
