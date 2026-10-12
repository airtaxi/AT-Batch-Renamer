using BatchRenamerAT.Enums;
using BatchRenamerAT.Models;
using System.Buffers;
using System.Text.RegularExpressions;

namespace BatchRenamerAT.Services;

// Computes the new name of every list item and flags names that cannot be applied.
public sealed class RenamePlanService
{
    private static readonly SearchValues<char> s_invalidFileNameCharacters = SearchValues.Create(Path.GetInvalidFileNameChars());

    private static readonly HashSet<string> s_reservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    public List<RenamePlanItem> CreatePlan(IReadOnlyList<RenameSource> sources, IReadOnlyList<RenameTrigger> triggers, CancellationToken cancellationToken)
    {
        RenameTriggerProcessor renameTriggerProcessor;
        try { renameTriggerProcessor = new RenameTriggerProcessor(triggers); }
        catch (ArgumentException exception) { return [.. sources.Select(source => CreateUnchangedItem(source, RenamePreviewStatus.TriggerError, exception.Message))]; }

        var planItems = new List<RenamePlanItem>(sources.Count);
        foreach (var source in sources)
        {
            cancellationToken.ThrowIfCancellationRequested();
            planItems.Add(CreatePlanItem(renameTriggerProcessor, source));
        }

        MarkConflicts(planItems);
        return planItems;
    }

    private static RenamePlanItem CreatePlanItem(RenameTriggerProcessor renameTriggerProcessor, RenameSource source)
    {
        var sourceExists = source.IsDirectory ? Directory.Exists(source.Path) : File.Exists(source.Path);
        if (!sourceExists) return CreateUnchangedItem(source, RenamePreviewStatus.Missing);

        var originalName = Path.GetFileName(source.Path);
        string newName;
        try { newName = renameTriggerProcessor.Apply(originalName, source.IsDirectory); }
        catch (RegexMatchTimeoutException exception) { return CreateUnchangedItem(source, RenamePreviewStatus.TriggerError, exception.Message); }

        return new RenamePlanItem(source, originalName, newName, GetNameStatus(originalName, newName));
    }

    private static RenamePlanItem CreateUnchangedItem(RenameSource source, RenamePreviewStatus status, string? errorMessage = null)
    {
        var originalName = Path.GetFileName(source.Path);
        return new RenamePlanItem(source, originalName, originalName, status, errorMessage);
    }

    private static RenamePreviewStatus GetNameStatus(string originalName, string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) return RenamePreviewStatus.EmptyName;
        if (newName.AsSpan().ContainsAny(s_invalidFileNameCharacters) || newName.EndsWith('.') || newName.EndsWith(' ')) return RenamePreviewStatus.InvalidCharacters;
        if (IsReservedName(newName)) return RenamePreviewStatus.ReservedName;

        return string.Equals(originalName, newName, StringComparison.Ordinal) ? RenamePreviewStatus.Unchanged : RenamePreviewStatus.Changed;
    }

    // Windows reserves device names with or without an extension ("CON", "con.txt").
    private static bool IsReservedName(string name)
    {
        var dotIndex = name.IndexOf('.');
        var baseName = dotIndex < 0 ? name : name[..dotIndex];
        return s_reservedNames.Contains(baseName.TrimEnd());
    }

    // Flags changed items whose target collides with another item of the batch, or with an
    // existing item on disk that the batch does not rename away.
    private static void MarkConflicts(List<RenamePlanItem> planItems)
    {
        var targetPaths = new string?[planItems.Count];
        var targetPathCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var sourcePaths = new HashSet<string>(planItems.Select(planItem => planItem.Source.Path), StringComparer.OrdinalIgnoreCase);

        for (var planItemIndex = 0; planItemIndex < planItems.Count; planItemIndex++)
        {
            var planItem = planItems[planItemIndex];
            if (planItem.Status is not (RenamePreviewStatus.Changed or RenamePreviewStatus.Unchanged)) continue;

            var targetPath = Path.Combine(Path.GetDirectoryName(planItem.Source.Path) ?? "", planItem.NewName);
            targetPaths[planItemIndex] = targetPath;
            targetPathCounts[targetPath] = targetPathCounts.GetValueOrDefault(targetPath) + 1;
        }

        for (var planItemIndex = 0; planItemIndex < planItems.Count; planItemIndex++)
        {
            var planItem = planItems[planItemIndex];
            if (planItem.Status != RenamePreviewStatus.Changed || targetPaths[planItemIndex] is not { } targetPath) continue;

            if (targetPathCounts[targetPath] > 1) planItems[planItemIndex] = planItem with { Status = RenamePreviewStatus.Duplicate };
            else if (!sourcePaths.Contains(targetPath) && (File.Exists(targetPath) || Directory.Exists(targetPath))) planItems[planItemIndex] = planItem with { Status = RenamePreviewStatus.AlreadyExists };
        }
    }
}
