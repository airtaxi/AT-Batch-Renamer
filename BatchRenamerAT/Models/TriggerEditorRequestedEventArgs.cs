using BatchRenamerAT.ViewModels;

namespace BatchRenamerAT.Models;

// Asks the host view to show the trigger editor for the given dialog view model; the result is
// true when the user confirmed the dialog.
public sealed class TriggerEditorRequestedEventArgs(TriggerEditorDialogViewModel dialogViewModel) : EventArgs
{
    public TriggerEditorDialogViewModel DialogViewModel { get; } = dialogViewModel;

    public Task<bool>? ResultTask { get; set; }
}
