namespace CtfDeck.Terminal.Terminal;

public sealed class TerminalService
{
    private readonly TerminalExecutor _executor = new();

    public Task<CommandResult> ExecuteStreamingAsync(
        string command,
        OutputReceivedHandler onOutput,
        CancellationToken ct,
        string? sudoPassword = null)
    {
        return _executor.ExecuteStreamingAsync(command, onOutput, ct, sudoPassword);
    }
}