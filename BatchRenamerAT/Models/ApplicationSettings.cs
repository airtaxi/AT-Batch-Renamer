using BatchRenamerAT.Enums;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using System.Text.Json.Serialization;

namespace BatchRenamerAT.Models;

public sealed partial class ApplicationSettings : ObservableObject
{
    [ObservableProperty]
    public partial ElementTheme Theme { get; set; } = ElementTheme.Default;

    [ObservableProperty]
    public partial string LanguageOverride { get; set; } = "";

    // When true, adding folders asks how to handle them; otherwise DefaultFolderHandlingMode is used.
    [ObservableProperty]
    public partial bool AskFolderHandling { get; set; } = true;

    [ObservableProperty]
    public partial FolderHandlingMode DefaultFolderHandlingMode { get; set; } = FolderHandlingMode.RenameFolders;
}

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ApplicationSettings))]
internal sealed partial class ApplicationSettingsJsonContext : JsonSerializerContext;
