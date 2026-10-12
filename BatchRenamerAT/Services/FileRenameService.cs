using BatchRenamerAT.Models;

namespace BatchRenamerAT.Services;

// Executes rename operations on disk.
//
// Every item is moved to a unique temporary name first and to its target name second, so swaps
// (a -> b, b -> a), chains and case-only renames succeed. Deeper paths are processed before their
// parents, which keeps child paths valid until their parent folder is renamed.
public sealed class FileRenameService
{
    public List<RenameOperationResult> Execute(IReadOnlyList<RenameOperation> operations)
    {
        var results = operations.Select(operation => new RenameOperationResult(operation)).ToList();
        foreach (var depthGroup in results.GroupBy(result => GetPathDepth(result.Operation.SourcePath)).OrderByDescending(group => group.Key)) ExecuteDepthGroup([..depthGroup], results);
        return results;
    }

    private static void ExecuteDepthGroup(List<RenameOperationResult> groupResults, List<RenameOperationResult> allResults)
    {
        var temporaryPaths = new List<(RenameOperationResult Result, string TemporaryPath)>();
        foreach (var result in groupResults)
        {
            var temporaryPath = Path.Combine(Path.GetDirectoryName(result.CurrentPath) ?? "", $"~{Guid.NewGuid():N}.tmp");
            try
            {
                Move(result.CurrentPath, temporaryPath, result.Operation.IsDirectory);
                temporaryPaths.Add((result, temporaryPath));
            }
            catch (Exception exception) when (IsFileSystemException(exception)) { result.ErrorMessage = exception.Message; }
        }

        foreach (var (result, temporaryPath) in temporaryPaths)
        {
            var isDirectory = result.Operation.IsDirectory;
            var targetPath = Path.Combine(Path.GetDirectoryName(result.CurrentPath) ?? "", result.Operation.TargetName);
            try
            {
                Move(temporaryPath, targetPath, isDirectory);
                var previousPath = result.CurrentPath;
                result.CurrentPath = targetPath;
                result.IsSuccessful = true;
                if (isDirectory) UpdateDescendantPaths(allResults, previousPath, targetPath);
            }
            catch (Exception exception) when (IsFileSystemException(exception))
            {
                result.ErrorMessage = exception.Message;

                // Restores the original name; if that fails as well the item stays at the temporary path.
                try { Move(temporaryPath, result.CurrentPath, isDirectory); }
                catch (Exception restoreException) when (IsFileSystemException(restoreException)) { result.CurrentPath = temporaryPath; }
            }
        }
    }

    private static void Move(string sourcePath, string targetPath, bool isDirectory)
    {
        if (isDirectory) Directory.Move(sourcePath, targetPath);
        else File.Move(sourcePath, targetPath, false);
    }

    private static void UpdateDescendantPaths(List<RenameOperationResult> results, string previousDirectoryPath, string newDirectoryPath)
    {
        var previousDirectoryPrefix = previousDirectoryPath + Path.DirectorySeparatorChar;
        foreach (var result in results)
        {
            if (result.CurrentPath.StartsWith(previousDirectoryPrefix, StringComparison.OrdinalIgnoreCase))
            {
                result.CurrentPath = newDirectoryPath + result.CurrentPath[previousDirectoryPath.Length..];
            }
        }
    }

    private static int GetPathDepth(string path) => path.Count(character => character == Path.DirectorySeparatorChar);

    private static bool IsFileSystemException(Exception exception) => exception is IOException or UnauthorizedAccessException or ArgumentException;
}
