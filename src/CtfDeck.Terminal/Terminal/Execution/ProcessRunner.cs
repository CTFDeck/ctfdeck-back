using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace CtfDeck.Terminal.Terminal;

/// <summary>
/// Manages process execution with streaming output support
/// </summary>
public static class ProcessRunner
{
    private static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan OutputDrainTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Runs a command in the specified shell with streaming output
    /// </summary>
    public static async Task<CommandResult> RunAsync(
        ShellConfig shell,
        string command,
        string workingDirectory,
        Func<string, bool, Task> onOutput,
        CancellationToken cancellationToken = default)
    {
        using var process = CreateProcess(shell, command, workingDirectory);

        var outputBuilder = new StringBuilder();
        var errorBuilder = new StringBuilder();
        var outputDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var errorDone = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        SetupOutputHandlers(process, outputBuilder, errorBuilder, outputDone, errorDone, onOutput);

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var exitCode = await WaitForCompletionAsync(process, outputDone, errorDone, cancellationToken);

        return new CommandResult
        {
            Output = outputBuilder.ToString(),
            Error = exitCode == -1 ? "Command timed out" : errorBuilder.ToString(),
            ExitCode = exitCode,
            WorkingDirectory = workingDirectory
        };
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

    private static void ConfigureEnvironment(ProcessStartInfo startInfo)
    {
        startInfo.Environment["PATH"] = Environment.GetEnvironmentVariable("PATH");
        startInfo.Environment["TERM"] = "xterm-256color";
        startInfo.Environment["COLORTERM"] = "truecolor";
        startInfo.Environment["CLICOLOR_FORCE"] = "1";
    }

    private static void SetupOutputHandlers(
        Process process,
        StringBuilder outputBuilder,
        StringBuilder errorBuilder,
        TaskCompletionSource<bool> outputDone,
        TaskCompletionSource<bool> errorDone,
        Func<string, bool, Task> onOutput)
    {
        process.OutputDataReceived += async (_, e) =>
        {
            if (e.Data is null)
            {
                outputDone.TrySetResult(true);
                return;
            }

            outputBuilder.AppendLine(e.Data);
            await SafeInvokeCallback(onOutput, e.Data + "\n", false);
        };

        process.ErrorDataReceived += async (_, e) =>
        {
            if (e.Data is null)
            {
                errorDone.TrySetResult(true);
                return;
            }

            errorBuilder.AppendLine(e.Data);
            await SafeInvokeCallback(onOutput, e.Data + "\n", true);
        };
    }

    private static async Task SafeInvokeCallback(Func<string, bool, Task> callback, string data, bool isError)
    {
        try
        {
            await callback(data, isError);
        }
        catch
        {
            // Callback errors shouldn't affect process execution
        }
    }

    private static async Task<int> WaitForCompletionAsync(
        Process process,
        TaskCompletionSource<bool> outputDone,
        TaskCompletionSource<bool> errorDone,
        CancellationToken cancellationToken)
    {
        using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            await process.WaitForExitAsync(linkedCts.Token);

            // Wait for output handlers to drain
            await Task.WhenAll(
                Task.WhenAny(outputDone.Task, Task.Delay(OutputDrainTimeout, CancellationToken.None)),
                Task.WhenAny(errorDone.Task, Task.Delay(OutputDrainTimeout, CancellationToken.None))
            );

            return process.ExitCode;
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            return -1;
        }
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch
        {
            // Process may have already exited
        }
    }
}
