using BatchRenamerAT.Enums;

namespace BatchRenamerAT.Models;

public sealed record InsertTrigger : RenameTrigger
{
    public InsertPosition Position { get; init; } = InsertPosition.End;

    // Number of characters kept before the inserted content when Position is AfterCharacter.
    public int CharacterIndex { get; init; }

    public TriggerContent Content { get; init; } = new();
}
