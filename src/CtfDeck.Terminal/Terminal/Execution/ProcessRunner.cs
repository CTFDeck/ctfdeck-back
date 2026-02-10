using System.Diagnostics;
using System.Text;

namespace CtfDeck.Terminal.Terminal;

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
        var escaped = command.Replace("\"", "\\\"");

        var psi = new ProcessStartInfo
        {
            FileName = shell.Executable,
            Arguments = $"{shell.ArgumentPrefix} \"{escaped}\"",
            WorkingDirectory = workingDirectory,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var p = new Process { StartInfo = psi };

        try
        {
            p.Start();

            if (writeStdin != null)
            {
                try
                {
                    await writeStdin(p.StandardInput);
                    await p.StandardInput.FlushAsync();
                    p.StandardInput.Close();
                }
                catch { }
            }

            var stdoutTask = PumpAsync(p.StandardOutput, onOutput, isError: false, cancellationToken);
            var stderrTask = PumpAsync(p.StandardError, onOutput, isError: true, cancellationToken);

            using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

            try { await p.WaitForExitAsync(linked.Token); }
            catch { try { p.Kill(entireProcessTree: true); } catch { } }

            await Task.WhenAll(stdoutTask, stderrTask);

            var exit = p.HasExited ? p.ExitCode : -1;
            return new CommandResult
            {
                ExitCode = exit,
                Output = "",   // ton OutputBatcher gère déjà l’affichage, sinon accumule ici
                Error = exit == -1 ? "Command timed out" : "",
                WorkingDirectory = workingDirectory
            };
        }
        catch (Exception ex)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            return CommandResult.Failure(workingDirectory, ex.Message, -1);
        }
    }

    private static async Task PumpAsync(
        StreamReader reader,
        Func<string, bool, Task> onOutput,
        bool isError,
        CancellationToken ct)
    {
        char[] buf = new char[4096];
        while (!reader.EndOfStream && !ct.IsCancellationRequested)
        {
            var read = await reader.ReadAsync(buf, 0, buf.Length);
            if (read > 0)
                await onOutput(new string(buf, 0, read), isError);
        }
    }
}
