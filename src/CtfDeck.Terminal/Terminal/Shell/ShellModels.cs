namespace CtfDeck.Terminal.Terminal.Shell;

/// <summary>
/// Represents a shell configuration for command execution
/// </summary>
public enum ShellType
{
    Auto,
    Bash,
    Cmd,
    PowerShell
}

/// <summary>
/// Shell configuration with executable path and arguments
/// </summary>
public sealed record ShellConfig(string Executable, string ArgumentPrefix);
