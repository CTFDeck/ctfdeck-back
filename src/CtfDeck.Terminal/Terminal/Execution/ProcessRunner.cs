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
        Action<StreamWriter>? onStdinReady = null,
        bool usePseudoTerminal = false,
        CancellationToken cancellationToken = default)
    {
        using var process = CreateProcess(shell, command, workingDirectory, usePseudoTerminal);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();

        try
        {
            process.Start();

            // If sudoPassword was provided, write it ASAP to stdin (kept open for EOF signal)
            if (writeStdin != null)
            {
                try
                {
                    await writeStdin(process.StandardInput);
                    await process.StandardInput.FlushAsync();
                }
                catch { /* ignore */ }
            }

            // Publish stdin to the caller so it can close it on EOF (Ctrl+D) signal
            try { onStdinReady?.Invoke(process.StandardInput); }
            catch { /* ignore */ }

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

        try
        {
            while (!ct.IsCancellationRequested)
            {
                int read = await reader.ReadAsync(buffer.AsMemory(), ct);

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
        catch (OperationCanceledException)
        {
            // Normal when process is killed or timed out
        }
        catch (Exception)
        {
            // Pipe might be closed
        }
    }

    private static Process CreateProcess(
        ShellConfig shell,
        string command,
        string workingDirectory,
        bool usePseudoTerminal)
    {
        var startInfo = new ProcessStartInfo
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workingDirectory
        };

        if (ShouldUsePseudoTerminal(usePseudoTerminal))
        {
            ConfigurePseudoTerminalProcess(startInfo, shell, command);
        }
        else
        {
            startInfo.FileName = shell.Executable;
            startInfo.ArgumentList.Add(shell.ArgumentPrefix);
            startInfo.ArgumentList.Add(command);
        }

        ConfigureEnvironment(startInfo);

        return new Process { StartInfo = startInfo };
    }

    private static bool ShouldUsePseudoTerminal(bool requested)
    {
        return (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            && IsExecutableOnPath("script")
            && requested;
    }

    private static void ConfigurePseudoTerminalProcess(
        ProcessStartInfo startInfo,
        ShellConfig shell,
        string command)
    {
        startInfo.FileName = "script";

        if (OperatingSystem.IsMacOS())
        {
            startInfo.ArgumentList.Add("-q");
            startInfo.ArgumentList.Add("/dev/null");
            startInfo.ArgumentList.Add(shell.Executable);
            startInfo.ArgumentList.Add(shell.ArgumentPrefix);
            startInfo.ArgumentList.Add(command);
            return;
        }

        startInfo.ArgumentList.Add("-q");
        startInfo.ArgumentList.Add("-f");
        startInfo.ArgumentList.Add("-e");
        startInfo.ArgumentList.Add("-c");
        startInfo.ArgumentList.Add($"{shell.Executable} {shell.ArgumentPrefix} {QuotePosix(command)}");
        startInfo.ArgumentList.Add("/dev/null");
    }

    private static bool IsExecutableOnPath(string executable)
    {
        var path = Environment.GetEnvironmentVariable("PATH");

        if (string.IsNullOrWhiteSpace(path))
            return false;

        foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var candidate = Path.Combine(directory, executable);

            if (File.Exists(candidate))
                return true;
        }

        return false;
    }

    private static string QuotePosix(string value)
    {
        return $"'{value.Replace("'", "'\"'\"'")}'";
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
        startInfo.Environment["LANG"] = "C.UTF-8";
        startInfo.Environment["LC_ALL"] = "C.UTF-8";

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
