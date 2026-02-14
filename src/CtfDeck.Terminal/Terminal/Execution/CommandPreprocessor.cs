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
    /// Separators we support for "chained" commands.
    /// We keep them in the output and recolorize each command segment.
    /// </summary>
    private static readonly string[] ChainSeparators = { "&&", "||", ";", "|" };

    /// <summary>
    /// Prepares a command for execution with the specified shell
    /// </summary>
    public static string Prepare(string command, ShellType shellType)
    {
        if (shellType != ShellType.Bash)
        {
            return command;
        }

        var colorizedCommand = InjectColorFlagsChained(command);
        return $"{BashColorEnv} {colorizedCommand}";
    }

    /// <summary>
    /// Injects color flags into supported commands, even when commands are chained
    /// (e.g. "cat a && ls -la; grep foo file | less").
    ///
    /// Notes:
    /// - This is a pragmatic splitter (not a full bash parser).
    /// - It does NOT understand quotes/subshells like: echo "a && b" or $(...)
    /// </summary>
    private static string InjectColorFlagsChained(string command)
    {
        var parts = SplitKeepSeparators(command, ChainSeparators);

        for (var i = 0; i < parts.Count; i++)
        {
            if (IsSeparator(parts[i])) continue;

            parts[i] = InjectColorFlags(parts[i]);
        }

        return string.Concat(parts);
    }

    private static bool IsSeparator(string s)
    {
        var t = s.Trim();
        return t is "&&" or "||" or ";" or "|";
    }

    private static List<string> SplitKeepSeparators(string input, string[] seps)
    {
        var result = new List<string>();
        var i = 0;

        while (i < input.Length)
        {
            var nextIndex = -1;
            var nextSep = "";

            foreach (var sep in seps)
            {
                var idx = input.IndexOf(sep, i, StringComparison.Ordinal);
                if (idx >= 0 && (nextIndex == -1 || idx < nextIndex))
                {
                    nextIndex = idx;
                    nextSep = sep;
                }
            }

            if (nextIndex == -1)
            {
                result.Add(input[i..]);
                break;
            }

            // chunk before separator
            if (nextIndex > i)
                result.Add(input[i..nextIndex]);

            // separator itself
            result.Add(nextSep);

            i = nextIndex + nextSep.Length;
        }

        return result;
    }

    /// <summary>
    /// Injects color flags into supported commands (single segment)
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
            // GNU ls uses --color; macOS/BSD ls uses -G (we fallback cleanly)
            "ls" => ReplaceFirst(command, "ls", "ls --color=always 2>/dev/null || ls -G"),
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
