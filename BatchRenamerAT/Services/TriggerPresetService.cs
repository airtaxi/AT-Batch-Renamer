using BatchRenamerAT.Models;
using System.Text.Json;

namespace BatchRenamerAT.Services;

// Saves and loads trigger lists as JSON trigger files.
public sealed class TriggerPresetService
{
    public const string FileExtension = ".brtriggers";

    public async Task SaveAsync(string filePath, IEnumerable<RenameTrigger> triggers)
    {
        var triggerPreset = new TriggerPreset { Triggers = [.. triggers] };
        await using var fileStream = File.Create(filePath);
        await JsonSerializer.SerializeAsync(fileStream, triggerPreset, TriggerPresetJsonContext.Default.TriggerPreset);
    }

    // Throws JsonException or NotSupportedException when the file is not a valid trigger file.
    public async Task<List<RenameTrigger>> LoadAsync(string filePath)
    {
        await using var fileStream = File.OpenRead(filePath);
        var triggerPreset = await JsonSerializer.DeserializeAsync(fileStream, TriggerPresetJsonContext.Default.TriggerPreset);
        return [.. triggerPreset?.Triggers.OfType<RenameTrigger>() ?? []];
    }
}
