using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using System.Text;
using BatchRenamerAT.Services;
using BatchRenamerAT.ViewModels;
using BatchRenamerAT.Views;

namespace BatchRenamerAT;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public App()
    {
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection);
        Services = serviceCollection.BuildServiceProvider();

        InitializeComponent();

        UnhandledException += OnApplicationUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnCurrentDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnTaskSchedulerUnobservedTaskException;
    }

    protected override void OnLaunched(LaunchActivatedEventArgs launchActivatedEventArguments)
    {
        var mainWindow = new MainWindow();
        mainWindow.Activate();
    }

    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        serviceCollection.AddSingleton<ApplicationSettingsService>();
        serviceCollection.AddSingleton(sp => sp.GetRequiredService<ApplicationSettingsService>().Settings);
        serviceCollection.AddSingleton(sp => new LocalizationService(sp.GetRequiredService<ApplicationSettingsService>().Settings.LanguageOverride));
        serviceCollection.AddSingleton(sp => new ApplicationThemeService(sp.GetRequiredService<ApplicationSettingsService>().Settings.Theme));
        serviceCollection.AddSingleton(_ => new RenamePlanService());
        serviceCollection.AddSingleton(_ => new FileRenameService());
        serviceCollection.AddSingleton(_ => new TriggerPresetService());
        serviceCollection.AddSingleton(sp => new TriggerDescriptionService(sp.GetRequiredService<LocalizationService>()));

        // The app runs one main window per process, so the panel view models shared by the main
        // page and its controls are singletons.
        serviceCollection.AddSingleton(sp => new FileListViewModel(sp.GetRequiredService<ApplicationSettingsService>(), sp.GetRequiredService<LocalizationService>()));
        serviceCollection.AddSingleton(sp => new TriggerListViewModel(sp.GetRequiredService<LocalizationService>(), sp.GetRequiredService<TriggerDescriptionService>(), sp.GetRequiredService<TriggerPresetService>()));
        serviceCollection.AddSingleton(sp => new RenamePreviewViewModel(sp.GetRequiredService<RenamePlanService>(), sp.GetRequiredService<LocalizationService>()));
        serviceCollection.AddTransient(sp => new MainPageViewModel(sp.GetRequiredService<FileListViewModel>(), sp.GetRequiredService<TriggerListViewModel>(), sp.GetRequiredService<RenamePreviewViewModel>(), sp.GetRequiredService<FileRenameService>(), sp.GetRequiredService<LocalizationService>()));
        serviceCollection.AddTransient(_ => new MainWindowViewModel());
        serviceCollection.AddTransient(sp => new SettingsWindowViewModel(sp.GetRequiredService<ApplicationSettingsService>(), sp.GetRequiredService<ApplicationThemeService>()));
    }

    private static void OnApplicationUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs unhandledExceptionEventArguments)
    {
        WriteException("Microsoft.UI.Xaml.Application.UnhandledException", unhandledExceptionEventArguments.Exception);
        unhandledExceptionEventArguments.Handled = true;
    }

    private static void OnCurrentDomainUnhandledException(object sender, System.UnhandledExceptionEventArgs unhandledExceptionEventArguments)
    {
        if (unhandledExceptionEventArguments.ExceptionObject is Exception exception)
        {
            WriteException("AppDomain.CurrentDomain.UnhandledException", exception);
        }
    }

    private static void OnTaskSchedulerUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs unobservedTaskExceptionEventArguments)
    {
        WriteException("TaskScheduler.UnobservedTaskException", unobservedTaskExceptionEventArguments.Exception);
        unobservedTaskExceptionEventArguments.SetObserved();
    }

    private static void WriteException(string source, Exception exception) => Debug.WriteLine(CreateExceptionMessage(source, exception));

    private static string CreateExceptionMessage(string source, Exception exception)
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append('[');
        stringBuilder.Append(source);
        stringBuilder.Append("] ");

        var currentException = exception;
        while (currentException is not null)
        {
            stringBuilder.Append(currentException.GetType().FullName);
            stringBuilder.Append(": ");
            stringBuilder.Append(currentException.Message);
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(currentException.StackTrace);

            currentException = currentException.InnerException;
            if (currentException is not null) stringBuilder.AppendLine("--->");
        }

        return stringBuilder.ToString();
    }
}