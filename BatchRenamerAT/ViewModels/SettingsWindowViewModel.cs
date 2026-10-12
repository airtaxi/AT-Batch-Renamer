using BatchRenamerAT.Enums;
using BatchRenamerAT.Helpers;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace BatchRenamerAT.ViewModels;

// Theme, language, and folder handling settings. Every change is saved immediately.
public sealed partial class SettingsWindowViewModel : BaseViewModel
{
    // Order matches the language ComboBox items; the empty tag follows the system language.
    private static readonly List<string> s_languageTags = ["", "en-US", "ko-KR", "ja-JP", "zh-Hans", "zh-Hant"];

    private readonly ApplicationSettingsService _applicationSettingsService;
    private readonly ApplicationThemeService _applicationThemeService;
    private readonly string _initialLanguageOverride;
    private readonly bool _isInitialized;

    public SettingsWindowViewModel(ApplicationSettingsService applicationSettingsService, ApplicationThemeService applicationThemeService)
    {
        _applicationSettingsService = applicationSettingsService;
        _applicationThemeService = applicationThemeService;

        var applicationSettings = applicationSettingsService.Settings;
        _initialLanguageOverride = applicationSettings.LanguageOverride;
        ThemeIndex = applicationSettings.Theme switch
        {
            ElementTheme.Light => 1,
            ElementTheme.Dark => 2,
            _ => 0
        };
        LanguageIndex = Math.Max(s_languageTags.IndexOf(applicationSettings.LanguageOverride), 0);
        FolderHandlingIndex = applicationSettings.AskFolderHandling ? 0 : (int)applicationSettings.DefaultFolderHandlingMode + 1;

        _isInitialized = true;
    }

    public string VersionText { get; } = CreateVersionText();

    [ObservableProperty]
    public partial int ThemeIndex { get; set; }

    [ObservableProperty]
    public partial int LanguageIndex { get; set; }

    // 0 asks every time; the other indices map to FolderHandlingMode values shifted by one.
    [ObservableProperty]
    public partial int FolderHandlingIndex { get; set; }

    [ObservableProperty]
    public partial bool IsRestartNoticeVisible { get; private set; }

    partial void OnThemeIndexChanged(int value)
    {
        if (!_isInitialized) return;

        var theme = value switch
        {
            1 => ElementTheme.Light,
            2 => ElementTheme.Dark,
            _ => ElementTheme.Default
        };
        _applicationSettingsService.Settings.Theme = theme;
        _applicationSettingsService.SaveSettings();
        _applicationThemeService.ApplyTheme(theme);
    }

    // The new language applies to resources loaded after a restart.
    partial void OnLanguageIndexChanged(int value)
    {
        if (!_isInitialized || value < 0 || value >= s_languageTags.Count) return;

        _applicationSettingsService.Settings.LanguageOverride = s_languageTags[value];
        _applicationSettingsService.SaveSettings();
        IsRestartNoticeVisible = s_languageTags[value] != _initialLanguageOverride;
    }

    partial void OnFolderHandlingIndexChanged(int value)
    {
        if (!_isInitialized || value < 0) return;

        var applicationSettings = _applicationSettingsService.Settings;
        applicationSettings.AskFolderHandling = value == 0;
        if (value > 0) applicationSettings.DefaultFolderHandlingMode = (FolderHandlingMode)(value - 1);
        _applicationSettingsService.SaveSettings();
    }

    private static string CreateVersionText()
    {
        if (!AppIdentityHelper.IsPackaged()) return "";

        var packageVersion = Package.Current.Id.Version;
        return $"{packageVersion.Major}.{packageVersion.Minor}.{packageVersion.Build}.{packageVersion.Revision}";
    }
}
