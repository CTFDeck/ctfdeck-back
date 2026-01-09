namespace CtfDeck.Terminal.Terminal;

/// <summary>
/// Interactive terminal service for running commands in a REPL loop
/// </summary>
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
    /// Runs the interactive terminal loop
    /// </summary>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _output.WriteAsync(_executor.GetPrompt());

            var command = await _input.ReadLineAsync(cancellationToken);

            if (ShouldExit(command))
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(command))
            {
                continue;
            }

            await ExecuteAndDisplayAsync(command, cancellationToken);
        }
    }

    private static bool ShouldExit(string? command)
    {
        if (command is null)
        {
            return true;
        }

        var trimmed = command.Trim();
        return ExitCommands.Contains(trimmed);
    }

    private async Task ExecuteAndDisplayAsync(string command, CancellationToken cancellationToken)
    {
        var result = await _executor.ExecuteStreamingAsync(
            command,
            async (data, isError) =>
            {
                var writer = isError ? _error : _output;
                await writer.WriteAsync(data);
            },
            cancellationToken
        );

        // For non-streaming output that wasn't displayed
        if (!string.IsNullOrEmpty(result.Output))
        {
            await _output.WriteAsync(result.Output);
        }

        if (!string.IsNullOrEmpty(result.Error))
        {
            await _error.WriteAsync(result.Error);
        }
    }

    public void Dispose()
    {
        if (_ownsExecutor)
        {
            _executor.Dispose();
        }
    }
}
