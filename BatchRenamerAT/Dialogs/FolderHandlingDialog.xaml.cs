using BatchRenamerAT.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BatchRenamerAT.Dialogs;

public sealed partial class FolderHandlingDialog : ContentDialog
{
    public FolderHandlingDialog(FolderHandlingDialogViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }

    public FolderHandlingDialogViewModel ViewModel { get; }
}
