namespace BatchRenamerAT.Models;

public sealed class RenameOperationResult(RenameOperation operation)
{
    public RenameOperation Operation { get; } = operation;

    public string OriginalName { get; } = Path.GetFileName(operation.SourcePath);

    // Current location of the item. Starts at the source path and follows renames of the item
    // itself and of any renamed parent folder in the same batch.
    public string CurrentPath { get; set; } = operation.SourcePath;

    public bool IsSuccessful { get; set; }

    public string? ErrorMessage { get; set; }
}
