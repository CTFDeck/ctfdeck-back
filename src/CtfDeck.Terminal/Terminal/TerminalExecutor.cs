using System.Diagnostics;
using System.Runtime.InteropServices;

namespace CtfDeck.Terminal.Terminal;

public class TerminalExecutor
{
    private string _currentDirectory;

    public TerminalExecutor()
    {
        _currentDirectory = Environment.CurrentDirectory;
    }

    public async Task<CommandResult> ExecuteAsync(string command)
    {
        try
        {
            string shell;
            string shellArg;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                shell = "cmd.exe";
                shellArg = "/c";
            }
            else
            {
                shell = "/bin/bash";
                shellArg = "-c";
            }

            if (command.Trim().StartsWith("cd "))
            {
                return HandleCdCommand(command);
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = shell,
                Arguments = $"{shellArg} \"{command.Replace("\"", "\\\"")}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = _currentDirectory
            };

            startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH");

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
