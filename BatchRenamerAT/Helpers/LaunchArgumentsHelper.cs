namespace BatchRenamerAT.Helpers;

// Reads the item paths passed on launch. The Explorer context menu writes the selected paths to
// a temporary list file ("--file-list <path>") to avoid the command-line length limit; plain
// path arguments are accepted as well.
public static class LaunchArgumentsHelper
{
    private const string FileListArgument = "--file-list";

    public static string[] GetLaunchPaths()
    {
        var commandLineArguments = Environment.GetCommandLineArgs();
        if (commandLineArguments.Length > 2 && commandLineArguments[1] == FileListArgument) return ReadFileList(commandLineArguments[2]);
        return commandLineArguments.Length > 1 ? commandLineArguments[1..] : [];
    }

    private static string[] ReadFileList(string fileListPath)
    {
        if (!File.Exists(fileListPath)) return [];

        string[] paths;
        try { paths = [.. File.ReadAllLines(fileListPath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim())]; }
        catch (IOException) { return []; }

        try { File.Delete(fileListPath); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }

        return paths;
    }
}
