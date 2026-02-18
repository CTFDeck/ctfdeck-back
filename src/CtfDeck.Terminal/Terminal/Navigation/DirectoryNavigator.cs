using System.Runtime.InteropServices;

namespace CtfDeck.Terminal.Terminal.Navigation;

/// <summary>
/// Handles directory navigation and path resolution
/// </summary>
public sealed class DirectoryNavigator
{
    private string _currentDirectory;

    public string CurrentDirectory => _currentDirectory;

    public DirectoryNavigator(string? initialDirectory = null)
    {
        _currentDirectory = initialDirectory ?? Environment.CurrentDirectory;
    }

    /// <summary>
    /// Changes the current directory
    /// </summary>
    /// <returns>Result indicating success or failure with error message</returns>
    public async Task<CommandResult> ChangeDirectoryAsync(string? path, Func<string, bool, Task> onOutput)
    {
        var targetPath = ResolvePath(path);

        if (targetPath is null)
        {
            return await CreateErrorResult("cd: Invalid path", onOutput);
        }

        if (!Directory.Exists(targetPath))
        {
            return await CreateErrorResult($"cd: {targetPath}: No such file or directory", onOutput);
        }

        _currentDirectory = targetPath;
        return CommandResult.Success(_currentDirectory);
    }

    /// <summary>
    /// Resolves a path relative to the current directory
    /// </summary>
    private string? ResolvePath(string? path)
    {
        // Empty path or ~ means home directory
        if (string.IsNullOrWhiteSpace(path) || path == "~")
        {
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        // Handle ~/ paths
        if (path.StartsWith("~/") || path.StartsWith("~\\"))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            path = Path.Combine(home, path[2..]);
        }

        // Handle relative paths
        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(_currentDirectory, path);
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return null;
        }
    }

    private async Task<CommandResult> CreateErrorResult(string error, Func<string, bool, Task> onOutput)
    {
        try
        {
            await onOutput(error + "\n", true);
        }
        catch { }

        return CommandResult.Failure(_currentDirectory, error);
    }

    /// <summary>
    /// Gets a formatted prompt string
    /// </summary>
    public string GetPrompt()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var displayPath = _currentDirectory.Replace(home, "~");

        if (ShellDetector.IsWindows)
        {
            return $"{displayPath}> ";
        }

        var username = Environment.UserName;
        var hostname = Environment.MachineName;
        var symbol = username == "root" ? "#" : "$";

        return $"{username}@{hostname}:{displayPath}{symbol} ";
    }
}
