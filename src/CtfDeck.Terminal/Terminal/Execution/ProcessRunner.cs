using System.Diagnostics;
using System.Text;
using CtfDeck.Terminal.Terminal.Core;
using CtfDeck.Terminal.Terminal.Shell;

namespace CtfDeck.Terminal.Terminal.Execution;

public static class ProcessRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);

    public static async Task<CommandResult> RunAsync(
        ShellConfig shell,
        string command,
        string workingDirectory,
        Func<string, bool, Task> onOutput,
        Func<StreamWriter, Task>? writeStdin = null,
        CancellationToken cancellationToken = default)
    {
        using var process = CreateProcess(shell, command, workingDirectory);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            process.Start();

            // If sudoPassword was provided, write it ASAP to stdin
            if (writeStdin != null)
            {
                try
                {
                    await writeStdin(process.StandardInput);
                    await process.StandardInput.FlushAsync();
                    process.StandardInput.Close();
                }
                catch { /* ignore */ }
            }

            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            var stdoutTask = PumpAsync(process.StandardOutput, outputBuilder, onOutput, isError: false, linkedCts.Token);
            var stderrTask = PumpAsync(process.StandardError, errorBuilder, onOutput, isError: true, linkedCts.Token);

            try
            {
                await process.WaitForExitAsync(linkedCts.Token);
            }
            catch (OperationCanceledException)
            {
                KillProcess(process);
                return new CommandResult
                {
                    Output = outputBuilder.ToString(),
                    Error = "Command timed out",
                    ExitCode = -1,
                    WorkingDirectory = workingDirectory
                };
            }

            await Task.WhenAll(stdoutTask, stderrTask);

            var exitCode = process.ExitCode;

            return new CommandResult
            {
                Output = outputBuilder.ToString(),
                Error = errorBuilder.ToString(),
                ExitCode = exitCode,
                WorkingDirectory = workingDirectory
            };
        }
        catch (Exception ex)
        {
            KillProcess(process);
            return CommandResult.Failure(workingDirectory, ex.Message, -1);
        }
    }

    private static async Task PumpAsync(
        StreamReader reader,
        StringBuilder builder,
        Func<string, bool, Task> onOutput,
        bool isError,
        CancellationToken ct)
    {
        var buffer = new char[4096];

        while (!ct.IsCancellationRequested)
        {
            int read;
            try
            {
                read = await reader.ReadAsync(buffer, 0, buffer.Length);
            }
            catch
            {
                break;
            }

            if (read <= 0) break;

            var chunk = new string(buffer, 0, read);
            builder.Append(chunk);

            try
            {
                await onOutput(chunk, isError);
            }
            catch
            { /* ignore */ }
        }
    }

    private static Process CreateProcess(ShellConfig shell, string command, string workingDirectory)
    {
        var escapedCommand = command.Replace("\"", "\\\"");

        var startInfo = new ProcessStartInfo
        {
            FileName = shell.Executable,
            Arguments = $"{shell.ArgumentPrefix} \"{escapedCommand}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        ConfigureEnvironment(startInfo);

        return new Process { StartInfo = startInfo };
    }

    private static string GetToolsBinDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CtfDeck",
                "tools",
                "bin");
        }

        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (OperatingSystem.IsMacOS())
        {
            return Path.Combine(
                home,
                "Library",
                "Application Support",
                "CtfDeck",
                "tools",
                "bin");
        }

        if (OperatingSystem.IsLinux())
        {
            return Path.Combine(
                home,
                ".local",
                "share",
                "ctfdeck",
                "tools",
                "bin");
        }

        throw new PlatformNotSupportedException("Unsupported OS.");
    }

    private static void ConfigureEnvironment(ProcessStartInfo startInfo)
    {
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? "";
        startInfo.Environment["PATH"] = currentPath;
        startInfo.Environment["TERM"] = "xterm-256color";
        startInfo.Environment["COLORTERM"] = "truecolor";
        startInfo.Environment["CLICOLOR_FORCE"] = "1";

        AddToolsBinToPath(startInfo, GetToolsBinDirectory());
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch { }
    }

    private static void AddToolsBinToPath(ProcessStartInfo psi, string toolsBinDirectory)
    {
        Directory.CreateDirectory(toolsBinDirectory);

        var currentPath =
            psi.Environment.TryGetValue("PATH", out var explicitPath) && !string.IsNullOrWhiteSpace(explicitPath)
                ? explicitPath
                : Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        var effectivePath = string.IsNullOrWhiteSpace(currentPath)
            ? toolsBinDirectory
            : $"{toolsBinDirectory}{Path.PathSeparator}{currentPath}";

        psi.Environment["PATH"] = effectivePath;
    }
}
