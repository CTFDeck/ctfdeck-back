namespace CtfDeck.Terminal.Terminal;

/// <summary>
/// Delegate for streaming output events
/// </summary>
public delegate Task OutputReceivedHandler(string data, bool isError);

/// <summary>
/// Terminal executor that provides command execution with streaming output.
/// This is the main entry point for terminal operations.
/// </summary>
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

    /// <summary>
    /// Sets the preferred shell type
    /// </summary>
    public void SetShell(ShellType shellType)
    {
        _shellType = shellType;
    }

    /// <summary>
    /// Executes a command with streaming output
    /// </summary>
    public Task<CommandResult> ExecuteStreamingAsync(
        string command,
        OutputReceivedHandler onOutput,
        CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_isDisposed, this);

        return CommandPreprocessor.IsDirectoryChangeCommand(command)
            ? ExecuteCdAsync(command, onOutput)
            : ExecuteShellCommandAsync(command, onOutput, cancellationToken);
    }

    /// <summary>
    /// Executes a command and returns the complete result (non-streaming)
    /// </summary>
    public async Task<CommandResult> ExecuteAsync(string command, CancellationToken cancellationToken = default)
    {
        // Use a simple callback that just accumulates output
        return await ExecuteStreamingAsync(
            command,
            static (_, _) => Task.CompletedTask,
            cancellationToken
        );
    }

    /// <summary>
    /// Gets a formatted prompt string for display
    /// </summary>
    public string GetPrompt() => _navigator.GetPrompt();

    private async Task<CommandResult> ExecuteCdAsync(string command, OutputReceivedHandler onOutput)
    {
        var path = CommandPreprocessor.ExtractCdPath(command);
        return await _navigator.ChangeDirectoryAsync(path, (data, isError) => onOutput(data, isError));
    }

    private async Task<CommandResult> ExecuteShellCommandAsync(
        string command,
        OutputReceivedHandler onOutput,
        CancellationToken cancellationToken)
    {
        try
        {
            var shell = ShellDetector.GetConfig(_shellType);
            var preparedCommand = CommandPreprocessor.Prepare(command, CurrentShell);

            return await ProcessRunner.RunAsync(
                shell,
                preparedCommand,
                _navigator.CurrentDirectory,
                (data, isError) => onOutput(data, isError),
                cancellationToken
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

    ~TerminalExecutor()
    {
        Dispose();
    }
}
