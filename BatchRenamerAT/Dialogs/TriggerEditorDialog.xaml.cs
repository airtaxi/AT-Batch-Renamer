using BatchRenamerAT.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace BatchRenamerAT.Dialogs;

public sealed partial class TriggerEditorDialog : ContentDialog
{
    public TriggerEditorDialog(TriggerEditorDialogViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }

    public TriggerEditorDialogViewModel ViewModel { get; }
}
