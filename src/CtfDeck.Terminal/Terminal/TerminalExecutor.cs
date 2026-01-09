using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections.Concurrent;

namespace CtfDeck.Terminal.Terminal;

public enum ShellType
{
    Auto,
    Bash,
    Cmd,
    PowerShell
}

/// <summary>
/// Delegate for streaming output events
/// </summary>
public delegate Task OutputReceivedHandler(string data, bool isError);

/// <summary>
/// A terminal executor that reuses shell processes for better performance.
/// Uses the async readline approach with DataReceived events for streaming.
/// </summary>
public class TerminalExecutor : IDisposable
{
    private string _currentDirectory;
    private ShellType _shellType = ShellType.Auto;
    private readonly string? _detectedBashPath;
    private bool _isDisposed;

    public string CurrentDirectory => _currentDirectory;

    public TerminalExecutor()
    {
        _currentDirectory = Environment.CurrentDirectory;
        _detectedBashPath = DetectAvailableShells();
    }

    private string? DetectAvailableShells()
    {
        var bashPath = "/bin/bash";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (File.Exists(bashPath))
            {
                return bashPath;
            }
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Check for Git Bash
            bashPath = @"C:\Program Files\Git\bin\bash.exe";
            if (File.Exists(bashPath))
            {
                return bashPath;
            }

            // Check for WSL bash
            bashPath = @"C:\Windows\System32\bash.exe";
            if (File.Exists(bashPath))
            {
                return bashPath;
            }
        }

        return null;
    }

    public bool IsBashAvailable => _detectedBashPath != null;

    public ShellType CurrentShell => _shellType == ShellType.Auto
        ? (IsBashAvailable ? ShellType.Bash : ShellType.Cmd)
        : _shellType;

    public void SetShell(ShellType shellType)
    {
        _shellType = shellType;
    }

    /// <summary>
    /// Execute command with streaming output.
    /// </summary>
    public async Task<CommandResult> ExecuteStreamingAsync(string command, OutputReceivedHandler onOutput)
    {
        try
        {
            // Handle cd command specially
            var trimmedCmd = command.Trim();
            if (trimmedCmd.StartsWith("cd ") || trimmedCmd == "cd")
            {
                return await ExecuteCdCommand(command, onOutput);
            }

            return await ExecuteWithStreaming(command, onOutput);
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Output = "",
                Error = $"Execution error: {ex.Message}",
                ExitCode = -1,
                WorkingDirectory = _currentDirectory
            };
        }
    }

    private async Task<CommandResult> ExecuteWithStreaming(string command, OutputReceivedHandler onOutput)
    {
        var effectiveShell = CurrentShell;
        string shell;
        string shellArg;
        string wrappedCommand = command;

        if (effectiveShell == ShellType.Bash && _detectedBashPath != null)
        {
            shell = _detectedBashPath;
            shellArg = "-c";

            // Inject color flags for common commands
            var finalCommand = command;
            var trimmed = command.TrimStart();
            if (trimmed.StartsWith("ls ") || trimmed == "ls")
            {
                finalCommand = command.Replace("ls", "ls --color=always", StringComparison.Ordinal);
            }
            else if (trimmed.StartsWith("grep ") || trimmed == "grep")
            {
                finalCommand = command.Replace("grep", "grep --color=always", StringComparison.Ordinal);
            }

            // Wrap command to enable colors
            var env = "export TERM=xterm-256color; export COLORTERM=truecolor; export CLICOLOR_FORCE=1; eval \"$(dircolors -b)\" 2>/dev/null || true;";
            wrappedCommand = $"{env} {finalCommand}";
        }
        else if (effectiveShell == ShellType.PowerShell)
        {
            shell = "powershell.exe";
            shellArg = "-Command";
        }
        else
        {
            shell = "cmd.exe";
            shellArg = "/c";
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = shell,
            Arguments = $"{shellArg} \"{wrappedCommand.Replace("\"", "\\\"")}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = _currentDirectory
        };

        startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH");
        startInfo.Environment["TERM"] = "xterm-256color";
        startInfo.Environment["COLORTERM"] = "truecolor";
        startInfo.Environment["CLICOLOR_FORCE"] = "1";

        using var process = new Process { StartInfo = startInfo };

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputComplete = new TaskCompletionSource<bool>();
        var errorComplete = new TaskCompletionSource<bool>();

        process.OutputDataReceived += async (sender, e) =>
        {
            if (e.Data == null)
            {
                outputComplete.TrySetResult(true);
                return;
            }
            outputBuilder.AppendLine(e.Data);
            try
            {
                await onOutput(e.Data + "\n", false);
            }
            catch { }
        };

        process.ErrorDataReceived += async (sender, e) =>
        {
            if (e.Data == null)
            {
                errorComplete.TrySetResult(true);
                return;
            }
            errorBuilder.AppendLine(e.Data);
            try
            {
                await onOutput(e.Data + "\n", true);
            }
            catch { }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // Wait for process to exit with timeout
        var exitTask = process.WaitForExitAsync();
        var timeoutTask = Task.Delay(TimeSpan.FromMinutes(5));
        
        var completedTask = await Task.WhenAny(exitTask, timeoutTask);
        
        if (completedTask == timeoutTask)
        {
            try { process.Kill(); } catch { }
            return new CommandResult
            {
                Output = outputBuilder.ToString(),
                Error = "Command timed out after 5 minutes",
                ExitCode = -1,
                WorkingDirectory = _currentDirectory
            };
        }

        // Wait a bit for the output handlers to finish
        await Task.WhenAll(
            Task.WhenAny(outputComplete.Task, Task.Delay(1000)),
            Task.WhenAny(errorComplete.Task, Task.Delay(1000))
        );

        return new CommandResult
        {
            Output = outputBuilder.ToString(),
            Error = errorBuilder.ToString(),
            ExitCode = process.ExitCode,
            WorkingDirectory = _currentDirectory
        };
    }

    private async Task<CommandResult> ExecuteCdCommand(string command, OutputReceivedHandler onOutput)
    {
        var path = command.Length > 3 ? command[3..].Trim() : "";

        if (string.IsNullOrEmpty(path) || path == "~")
        {
            path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        else if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(_currentDirectory, path);
        }

        // Normalize path
        try
        {
            path = Path.GetFullPath(path);
        }
        catch
        {
            var error = $"cd: Invalid path: {path}";
            await onOutput(error + "\n", true);
            return new CommandResult
            {
                Output = "",
                Error = error,
                ExitCode = 1,
                WorkingDirectory = _currentDirectory
            };
        }

        if (!Directory.Exists(path))
        {
            var error = $"cd: {path}: No such file or directory";
            await onOutput(error + "\n", true);
            return new CommandResult
            {
                Output = "",
                Error = error,
                ExitCode = 1,
                WorkingDirectory = _currentDirectory
            };
        }

        _currentDirectory = path;

        return new CommandResult
        {
            Output = "",
            Error = "",
            ExitCode = 0,
            WorkingDirectory = _currentDirectory
        };
    }

    /// <summary>
    /// Legacy non-streaming execution
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(string command)
    {
        var output = new StringBuilder();
        var error = new StringBuilder();

        var result = await ExecuteStreamingAsync(command, (data, isError) =>
        {
            if (isError)
                error.Append(data);
            else
                output.Append(data);
            return Task.CompletedTask;
        });

        return new CommandResult
        {
            Output = output.ToString(),
            Error = error.ToString(),
            ExitCode = result.ExitCode,
            WorkingDirectory = result.WorkingDirectory
        };
    }

    public string GetPrompt()
    {
        var username = Environment.UserName;
        var hostname = Environment.MachineName;
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var dir = _currentDirectory.Replace(home, "~");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return $"{dir}> ";
        }

        var isRoot = username == "root";
        var symbol = isRoot ? "#" : "$";
        return $"{username}@{hostname}:{dir}{symbol} ";
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~TerminalExecutor()
    {
        Dispose();
    }
}
