using BatchRenamerAT.Enums;

namespace BatchRenamerAT.Models;

// Text written by a replace or insert trigger: either fixed text or a sequence number that grows
// by Step for every item the trigger is applied to.
public sealed record TriggerContent
{
    public TriggerContentKind Kind { get; init; } = TriggerContentKind.Text;

    public string Text { get; init; } = "";

    // Minimum number of digits; shorter numbers are padded with leading zeros.
    public int Digits { get; init; } = 1;

    public long StartNumber { get; init; } = 1;

    public long Step { get; init; } = 1;
}
