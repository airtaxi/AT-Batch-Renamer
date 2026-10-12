using BatchRenamerAT.ViewModels;

namespace BatchRenamerAT.Models;

// Asks the host view how added folders should be handled; the result is true when the user
// confirmed the dialog.
public sealed class FolderHandlingRequestedEventArgs(FolderHandlingDialogViewModel dialogViewModel) : EventArgs
{
    public FolderHandlingDialogViewModel DialogViewModel { get; } = dialogViewModel;

    public Task<bool>? ResultTask { get; set; }
}
