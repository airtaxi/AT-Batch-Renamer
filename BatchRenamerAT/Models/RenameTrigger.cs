using BatchRenamerAT.Enums;
using System.Text.Json.Serialization;

namespace BatchRenamerAT.Models;

// One step of the rename pipeline. Triggers run in list order, and each trigger receives the
// name produced by the previous one.
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ReplaceTrigger), "replace")]
[JsonDerivedType(typeof(InsertTrigger), "insert")]
[JsonDerivedType(typeof(CaseConversionTrigger), "convertCase")]
public abstract record RenameTrigger
{
    public bool IsEnabled { get; init; } = true;

    public NameScope Scope { get; init; } = NameScope.FileName;

    // When set, the trigger only runs for names that satisfy the condition.
    public TriggerCondition? Condition { get; init; }
}
