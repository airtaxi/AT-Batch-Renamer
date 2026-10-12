using CommunityToolkit.Mvvm.ComponentModel;

namespace BatchRenamerAT.ViewModels;

public sealed partial class FileItemViewModel(string path, bool isDirectory) : ObservableObject
{
    // Updated after a rename so later previews and renames start from the new location.
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Name))]
    [NotifyPropertyChangedFor(nameof(DirectoryPath))]
    public partial string Path { get; set; } = path;

    public bool IsDirectory { get; } = isDirectory;

    public bool IsFile => !IsDirectory;

    public string Name => System.IO.Path.GetFileName(Path);

    public string DirectoryPath => System.IO.Path.GetDirectoryName(Path) ?? "";
}
