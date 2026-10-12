namespace BatchRenamerAT.Enums;

// Outcome of a planned or executed rename for one item.
public enum RenamePreviewStatus
{
    Changed,
    Unchanged,
    Duplicate,
    AlreadyExists,
    InvalidCharacters,
    EmptyName,
    ReservedName,
    Missing,
    TriggerError,
    Renamed,
    Failed
}
