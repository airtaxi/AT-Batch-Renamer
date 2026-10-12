using BatchRenamerAT.Enums;

namespace BatchRenamerAT.Models;

public sealed record CaseConversionTrigger : RenameTrigger
{
    public CaseConversionMode Mode { get; init; } = CaseConversionMode.Lowercase;
}
