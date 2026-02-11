namespace CtfDeck.Terminal.Terminal;

public delegate Task OutputReceivedHandler(string data, bool isError);

public sealed class TerminalExecutor : IDisposable
{
    private readonly DirectoryNavigator _navigator;
    private ShellType _shellType = ShellType.Auto;
    private bool _isDisposed;

    public string CurrentDirectory => _navigator.CurrentDirectory;
    public bool IsBashAvailable => ShellDetector.IsBashAvailable;
    public ShellType CurrentShell => ShellDetector.ResolveShellType(_shellType);

    public TerminalExecutor() : this(null) { }

    public TerminalExecutor(string? initialDirectory)
    {
        _navigator = new DirectoryNavigator(initialDirectory);
    }

    public void SetShell(ShellType shellType) => _shellType = shellType;

    public Task<CommandResult> ExecuteStreamingAsync(
        string command,
        OutputReceivedHandler onOutput,
        CancellationToken cancellationToken = default,
        string? sudoPassword = null)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        return CommandPreprocessor.IsDirectoryChangeCommand(command)
            ? ExecuteCdAsync(command, onOutput)
            : ExecuteShellCommandAsync(command, onOutput, cancellationToken, sudoPassword);
    }

    public async Task<CommandResult> ExecuteAsync(string command, CancellationToken cancellationToken = default)
    {
        return await ExecuteStreamingAsync(command, static (_, _) => Task.CompletedTask, cancellationToken, null);
    }

    public string GetPrompt() => _navigator.GetPrompt();

    private async Task<CommandResult> ExecuteCdAsync(string command, OutputReceivedHandler onOutput)
    {
        var path = CommandPreprocessor.ExtractCdPath(command);
        return await _navigator.ChangeDirectoryAsync(path, (d, e) => onOutput(d, e));
    }

    private static bool IsSudo(string cmd)
    {
        var t = cmd.TrimStart();
        return t == "sudo" || t.StartsWith("sudo ", StringComparison.Ordinal);
    }

    private static string PrepareSudo(string cmd)
    {
        if (!IsSudo(cmd)) return cmd;

        var trimmed = cmd.TrimStart();
        if (trimmed.StartsWith("sudo -S", StringComparison.Ordinal)) return cmd;

        var idx = cmd.IndexOf("sudo", StringComparison.Ordinal);
        return idx < 0 ? cmd : cmd[..idx] + "sudo -S -p ''" + cmd[(idx + 4)..];
    }

    private async Task<CommandResult> ExecuteShellCommandAsync(
        string command,
        OutputReceivedHandler onOutput,
        CancellationToken cancellationToken,
        string? sudoPassword)
    {
        try
        {
            var shell = ShellDetector.GetConfig(_shellType);

            var finalCmd = (sudoPassword != null && IsSudo(command))
                ? PrepareSudo(command)
                : command;

            var prepared = CommandPreprocessor.Prepare(finalCmd, CurrentShell);

            return await ProcessRunner.RunAsync(
                shell,
                prepared,
                _navigator.CurrentDirectory,
                (d, e) => onOutput(d, e),
                writeStdin: (sudoPassword != null && IsSudo(command))
                    ? (sw => sw.WriteLineAsync(sudoPassword))
                    : null,
                cancellationToken: cancellationToken
            );
        }
        catch (Exception ex)
        {
            return CommandResult.Failure(_navigator.CurrentDirectory, $"Execution error: {ex.Message}", -1);
        }
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        GC.SuppressFinalize(this);
    }

    ~TerminalExecutor() => Dispose();
}
