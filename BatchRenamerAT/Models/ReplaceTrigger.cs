using BatchRenamerAT.Enums;

namespace BatchRenamerAT.Models;

public sealed record ReplaceTrigger : RenameTrigger
{
    public ReplaceTarget Target { get; init; } = ReplaceTarget.SpecificText;

    public string FindText { get; init; } = "";

    public bool UseRegularExpression { get; init; }

    public bool IsCaseSensitive { get; init; }

    public MatchOccurrence Occurrence { get; init; } = MatchOccurrence.All;

    public TriggerContent Content { get; init; } = new();
}
