namespace BatchRenamerAT.Models;

// Snapshot of one list item used to compute the rename plan off the UI thread.
public sealed record RenameSource(string Path, bool IsDirectory);
