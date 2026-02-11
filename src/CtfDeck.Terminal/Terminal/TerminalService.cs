using System.Runtime.InteropServices;

namespace CtfDeck.Terminal.Terminal;

public sealed class TerminalService : IDisposable
{
    private readonly TerminalExecutor _executor;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly TextWriter _error;
    private readonly bool _ownsExecutor;

    private static readonly HashSet<string> ExitCommands = new(StringComparer.OrdinalIgnoreCase)
    {
        "exit", "quit", "q"
    };

    public TerminalService(
        TerminalExecutor? executor = null,
        TextReader? input = null,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        _executor = executor ?? new TerminalExecutor();
        _ownsExecutor = executor is null;

        _input = input ?? Console.In;
        _output = output ?? Console.Out;
        _error = error ?? Console.Error;
    }

    /// <summary>
    /// Interactive REPL loop (what your Xunit tests expect)
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _output.WriteAsync(_executor.GetPrompt());

            // If input ends (StringReader empty), ReadLineAsync returns null => terminate
            var line = await _input.ReadLineAsync(cancellationToken);
            if (line is null) break;

            var command = line.Trim();

            if (ExitCommands.Contains(command))
                break;

            if (string.IsNullOrWhiteSpace(command))
                continue;

            // Stream to output/error so tests can assert on writers
            var result = await _executor.ExecuteStreamingAsync(
                line, // keep original spacing (cd path etc.)
                async (data, isError) =>
                {
                    if (isError) await _error.WriteAsync(data);
                    else await _output.WriteAsync(data);
                },
                cancellationToken
            );

            // In case some implementations return aggregated strings too
            if (!string.IsNullOrEmpty(result.Output))
                await _output.WriteAsync(result.Output);

            if (!string.IsNullOrEmpty(result.Error))
                await _error.WriteAsync(result.Error);
        }
    }

    /// <summary>
    /// Programmatic API (WebSocket) - keeps sudo support
    /// </summary>
    public Task<CommandResult> ExecuteStreamingAsync(
        string command,
        OutputReceivedHandler onOutput,
        CancellationToken ct,
        string? sudoPassword = null)
    {
        return _executor.ExecuteStreamingAsync(command, onOutput, ct, sudoPassword);
    }

    public Task<CommandResult> ExecuteAsync(string command, CancellationToken ct = default)
        => _executor.ExecuteAsync(command, ct);

    public void Dispose()
    {
        if (_ownsExecutor)
            _executor.Dispose();
    }
}
