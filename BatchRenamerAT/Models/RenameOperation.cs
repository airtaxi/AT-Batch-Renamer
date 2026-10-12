namespace BatchRenamerAT.Models;

// Renames the item at SourcePath to TargetName inside the same folder.
public sealed record RenameOperation(string SourcePath, string TargetName, bool IsDirectory);
