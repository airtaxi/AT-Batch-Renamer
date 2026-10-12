using BatchRenamerAT.Enums;

namespace BatchRenamerAT.Models;

public sealed record RenamePlanItem(RenameSource Source, string OriginalName, string NewName, RenamePreviewStatus Status, string? ErrorMessage = null);
