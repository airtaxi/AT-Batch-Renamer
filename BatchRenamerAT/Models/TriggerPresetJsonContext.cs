using System.Text.Json.Serialization;

namespace BatchRenamerAT.Models;

[JsonSourceGenerationOptions(WriteIndented = true, UseStringEnumConverter = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TriggerPreset))]
internal sealed partial class TriggerPresetJsonContext : JsonSerializerContext;
