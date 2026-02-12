namespace CtfDeck.Terminal.Terminal;

/// <summary>
/// Prepares commands for shell execution with proper color support
/// </summary>
public static class CommandPreprocessor
{
    /// <summary>
    /// Environment setup for color support in bash
    /// </summary>
    private const string BashColorEnv =
        "export TERM=xterm-256color; export COLORTERM=truecolor; export CLICOLOR_FORCE=1; " +
        "command -v dircolors >/dev/null 2>&1 && eval \"$(dircolors -b)\" 2>/dev/null || true;";

    /// <summary>
    /// Commands that should have color flags injected
    /// </summary>
    private static readonly HashSet<string> ColorableCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "ls", "grep", "diff", "tree"
    };

    /// <summary>
    /// Prepares a command for execution with the specified shell
    /// </summary>
    public static string Prepare(string command, ShellType shellType)
    {
        if (shellType != ShellType.Bash)
        {
            return command;
        }

        var colorizedCommand = InjectColorFlags(command);
        return $"{BashColorEnv} {colorizedCommand}";
    }

    /// <summary>
    /// Injects color flags into supported commands
    /// </summary>
    private static string InjectColorFlags(string command)
    {
        var trimmed = command.TrimStart();
        var spaceIndex = trimmed.IndexOf(' ');
        var baseCommand = spaceIndex > 0 ? trimmed[..spaceIndex] : trimmed;

        if (!ColorableCommands.Contains(baseCommand))
        {
            return command;
        }

        // Replace first occurrence of the command with colored version
        return baseCommand.ToLowerInvariant() switch
        {
            "ls" => ReplaceFirst(command, "ls", "ls --color=always"),
            "grep" => ReplaceFirst(command, "grep", "grep --color=always"),
            "diff" => ReplaceFirst(command, "diff", "diff --color=always"),
            "tree" => ReplaceFirst(command, "tree", "tree -C"),
            _ => command
        };
    }

    /// <summary>
    /// Replaces the first occurrence of a string
    /// </summary>
    private static string ReplaceFirst(string text, string search, string replace)
    {
        var pos = text.IndexOf(search, StringComparison.Ordinal);
        if (pos < 0)
        {
            return text;
        }
        return string.Concat(text.AsSpan(0, pos), replace, text.AsSpan(pos + search.Length));
    }

    /// <summary>
    /// Checks if the command is a directory change command
    /// </summary>
    public static bool IsDirectoryChangeCommand(string command)
    {
        var trimmed = command.Trim();
        return trimmed.StartsWith("cd ", StringComparison.Ordinal) || trimmed == "cd";
    }

    /// <summary>
    /// Extracts the path from a cd command, returns null for just "cd"
    /// </summary>
    public static string? ExtractCdPath(string command)
    {
        var trimmed = command.Trim();
        if (trimmed.Length <= 3)
        {
            return null;
        }
        return trimmed[3..].Trim();
    }
}
