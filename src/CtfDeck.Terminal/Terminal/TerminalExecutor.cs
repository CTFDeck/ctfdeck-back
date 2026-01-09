using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CtfDeck.Terminal.Terminal;

public enum ShellType
{
    Auto,
    Bash,
    Cmd,
    PowerShell
}

public class TerminalExecutor
{
    private string _currentDirectory;
    private ShellType _shellType = ShellType.Auto;
    private string? _detectedBashPath;

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
            Console.WriteLine($"mac/linux; bashPath: {bashPath}");
            if (File.Exists(bashPath))
            {
                return bashPath;
            }
        }
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Check for Git Bash
            bashPath = @"C:\Program Files\Git\bin\bash.exe";
            Console.WriteLine($"windows; bashPath: {bashPath}");
            if (File.Exists(bashPath))
            {
                return bashPath;
            }

            // Check for WSL bash
            bashPath = @"C:\Windows\System32\bash.exe";
            Console.WriteLine($"windows; bashPath: {bashPath}");
            if (File.Exists(bashPath))
            {
                return bashPath;
            }
        }
        
        return null; // No bash found
    }

    public bool IsBashAvailable => _detectedBashPath != null;

    public ShellType CurrentShell => _shellType == ShellType.Auto 
        ? (IsBashAvailable ? ShellType.Bash : ShellType.Cmd) 
        : _shellType;

    public void SetShell(ShellType shellType)
    {
        _shellType = shellType;
    }

    public async Task<CommandResult> ExecuteAsync(string command)
    {
        try
        {
            if (command.Trim().StartsWith("cd "))
            {
                return HandleCdCommand(command);
            }

            var effectiveShell = CurrentShell;
            string shell;
            string shellArg;
            string wrappedCommand = command;

            if (effectiveShell == ShellType.Bash && _detectedBashPath != null)
            {
                shell = _detectedBashPath;
                shellArg = "-c";
                
                // Explicitly inject flags for common commands since aliases might fail in non-interactive shell
                var finalCommand = command;
                var trimmed = command.TrimStart();
                if (trimmed.StartsWith("ls ") || trimmed == "ls")
                {
                    finalCommand = command.Replace("ls", "ls --color=always");
                }
                else if (trimmed.StartsWith("grep ") || trimmed == "grep")
                {
                    finalCommand = command.Replace("grep", "grep --color=always");
                }

                // Wrap command to enable colors - include LS_COLORS
                var env = "export TERM=xterm-256color; export COLORTERM=truecolor; export CLICOLOR_FORCE=1; eval \"$(dircolors -b)\";";
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

            using var process = Process.Start(startInfo)!;

            var outputTask = process.StandardOutput.ReadToEndAsync();
            var errorTask = process.StandardError.ReadToEndAsync();

            await Task.WhenAll(outputTask, errorTask);

            process.WaitForExit();

            return new CommandResult
            {
                Output = outputTask.Result,
                Error = errorTask.Result,
                ExitCode = process.ExitCode,
                WorkingDirectory = _currentDirectory
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Output = "",
                Error = $"Runtime error: {ex.Message}",
                ExitCode = -1,
                WorkingDirectory = _currentDirectory
            };
        }
    }

    private CommandResult HandleCdCommand(string command)
    {
        try
        {
            var path = command.Substring(3).Trim();

            if (string.IsNullOrEmpty(path) || path == "~")
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            }
            else if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(_currentDirectory, path);
            }

            if (Directory.Exists(path))
            {
                _currentDirectory = Path.GetFullPath(path);
                return new CommandResult
                {
                    Output = "",
                    Error = "",
                    ExitCode = 0,
                    WorkingDirectory = _currentDirectory
                };
            }

            return new CommandResult
            {
                Output = "",
                Error = $"cd: {path}: No such file or directory",
                ExitCode = 1,
                WorkingDirectory = _currentDirectory
            };
        }
        catch (Exception ex)
        {
            return new CommandResult
            {
                Output = "",
                Error = $"cd: {ex.Message}",
                ExitCode = 1,
                WorkingDirectory = _currentDirectory
            };
        }
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
}
