using System.Runtime.InteropServices;

namespace CtfDeck.Terminal.Terminal;

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

/// <summary>
/// Detects and provides shell configurations for the current platform
/// </summary>
public static class ShellDetector
{
    private static readonly Lazy<string?> BashPath = new(DetectBashPath, LazyThreadSafetyMode.ExecutionAndPublication);

    /// <summary>
    /// Common bash paths to check on Windows
    /// </summary>
    private static readonly string[] WindowsBashPaths =
    [
        @"C:\Program Files\Git\bin\bash.exe",
        @"C:\Windows\System32\bash.exe",
        @"C:\Program Files (x86)\Git\bin\bash.exe"
    ];

    /// <summary>
    /// Common bash paths to check on Unix
    /// </summary>
    private static readonly string[] UnixBashPaths =
    [
        "/bin/bash",
        "/usr/bin/bash",
        "/usr/local/bin/bash"
    ];

    public static bool IsBashAvailable => BashPath.Value is not null;

    public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private static string? DetectBashPath()
    {
        var paths = IsWindows ? WindowsBashPaths : UnixBashPaths;

        foreach (var path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    /// <summary>
    /// Resolves the effective shell type based on preference and availability
    /// </summary>
    public static ShellType ResolveShellType(ShellType preference)
    {
        if (preference != ShellType.Auto)
        {
            return preference;
        }

        return IsBashAvailable ? ShellType.Bash : ShellType.Cmd;
    }

    /// <summary>
    /// Gets the shell configuration for the specified shell type
    /// </summary>
    public static ShellConfig GetConfig(ShellType shellType)
    {
        var resolved = ResolveShellType(shellType);

        return resolved switch
        {
            ShellType.Bash => new ShellConfig(BashPath.Value ?? throw new InvalidOperationException("Bash not available"), "-c"),
            ShellType.PowerShell => new ShellConfig("powershell.exe", "-Command"),
            ShellType.Cmd => new ShellConfig("cmd.exe", "/c"),
            _ => throw new ArgumentOutOfRangeException(nameof(shellType))
        };
    }
}
