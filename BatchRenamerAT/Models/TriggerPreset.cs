namespace BatchRenamerAT.Models;

// Root object of a saved trigger file.
public sealed record TriggerPreset
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public List<RenameTrigger> Triggers { get; init; } = [];
}
