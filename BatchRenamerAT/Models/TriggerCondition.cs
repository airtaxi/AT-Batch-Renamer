using BatchRenamerAT.Enums;

namespace BatchRenamerAT.Models;

// Condition evaluated against the name produced by the previous triggers.
public sealed record TriggerCondition
{
    public NameScope Scope { get; init; } = NameScope.FileName;

    public ConditionOperator Operator { get; init; } = ConditionOperator.Contains;

    public string Text { get; init; } = "";

    public bool IsCaseSensitive { get; init; }

    public ConditionMode Mode { get; init; } = ConditionMode.WhenMatches;
}
