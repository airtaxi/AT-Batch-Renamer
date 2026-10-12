using BatchRenamerAT.Enums;
using BatchRenamerAT.Services;
using CommunityToolkit.Mvvm.ComponentModel;

namespace BatchRenamerAT.ViewModels;

public sealed partial class FolderHandlingDialogViewModel(LocalizationService localizationService, int folderCount, FolderHandlingMode initialMode) : ObservableObject
{
    public string Description { get; } = localizationService.GetFormattedString("FolderHandlingDescriptionFormat", folderCount);

    [ObservableProperty]
    public partial int SelectedModeIndex { get; set; } = (int)initialMode;

    [ObservableProperty]
    public partial bool IsRememberChoiceChecked { get; set; }

    public FolderHandlingMode SelectedMode => Enum.IsDefined((FolderHandlingMode)SelectedModeIndex) ? (FolderHandlingMode)SelectedModeIndex : FolderHandlingMode.RenameFolders;
}
