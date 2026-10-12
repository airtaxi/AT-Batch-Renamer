using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using Microsoft.UI.Xaml;
using System.Text.Json;

namespace BatchRenamerAT.Services;

public sealed class ApplicationSettingsService
{
    private const string SettingsFileName = "settings.json";

    private static readonly string s_settingsFilePath = CreateSettingsFilePath();

    public ApplicationSettingsService() => Settings = LoadSettings();

    public event EventHandler? SettingsChanged;

    public ApplicationSettings Settings { get; }

    public void SaveSettings()
    {
        NormalizeSettings(Settings);
        var directoryPath = Path.GetDirectoryName(s_settingsFilePath)!;
        Directory.CreateDirectory(directoryPath);
        var json = JsonSerializer.Serialize(Settings, ApplicationSettingsJsonContext.Default.ApplicationSettings);
        File.WriteAllText(s_settingsFilePath, json);
        SettingsChanged?.Invoke(this, EventArgs.Empty);
    }

    private static ApplicationSettings LoadSettings()
    {
        try
        {
            if (File.Exists(s_settingsFilePath))
            {
                var json = File.ReadAllText(s_settingsFilePath);
                var applicationSettings = JsonSerializer.Deserialize(json, ApplicationSettingsJsonContext.Default.ApplicationSettings);
                if (applicationSettings is not null)
                {
                    NormalizeSettings(applicationSettings);
                    return applicationSettings;
                }
            }
        }
        catch (JsonException) { }
        catch (IOException) { }

        var fallbackSettings = new ApplicationSettings();
        NormalizeSettings(fallbackSettings);
        return fallbackSettings;
    }

    private static string CreateSettingsFilePath()
    {
        var rootNamespace = typeof(App).Namespace ?? nameof(BatchRenamerAT);
        var localAppDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppDataPath, rootNamespace, SettingsFileName);
    }

    private static void NormalizeSettings(ApplicationSettings applicationSettings)
    {
        applicationSettings.Theme = applicationSettings.Theme is ElementTheme.Default or ElementTheme.Light or ElementTheme.Dark ? applicationSettings.Theme : ElementTheme.Default;
        applicationSettings.LanguageOverride = NormalizeLanguageOverride(applicationSettings.LanguageOverride);
        applicationSettings.DefaultFolderHandlingMode = Enum.IsDefined(applicationSettings.DefaultFolderHandlingMode) ? applicationSettings.DefaultFolderHandlingMode : FolderHandlingMode.RenameFolders;
    }

    private static string NormalizeLanguageOverride(string languageOverride) => languageOverride is "ko-KR" or "en-US" or "ja-JP" or "zh-Hans" or "zh-Hant" ? languageOverride : "";
}