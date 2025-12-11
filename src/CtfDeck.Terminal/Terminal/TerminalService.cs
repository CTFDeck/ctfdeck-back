namespace CtfDeck.Terminal.Terminal;

public class TerminalService
{
    private readonly TerminalExecutor _executor;
    private readonly TextReader _input;
    private readonly TextWriter _output;
    private readonly TextWriter _error;

    public TerminalService(
        TerminalExecutor? executor = null,
        TextReader? input = null,
        TextWriter? output = null,
        TextWriter? error = null)
    {
        _executor = executor ?? new TerminalExecutor();
        _input = input ?? Console.In;
        _output = output ?? Console.Out;
        _error = error ?? Console.Error;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            _output.Write(_executor.GetPrompt());
            var command = await _input.ReadLineAsync();

            if (command is null || command.Trim() is "exit" or "quit")
                break;

            var result = await _executor.ExecuteAsync(command);

            if (!string.IsNullOrEmpty(result.Output))
                await _output.WriteLineAsync(result.Output);

            if (!string.IsNullOrEmpty(result.Error))
                await _error.WriteLineAsync(result.Error);
        }
    }
}
