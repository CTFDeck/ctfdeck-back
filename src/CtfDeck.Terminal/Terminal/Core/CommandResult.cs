namespace CtfDeck.Terminal.Terminal;

/// <summary>
/// Immutable result of a command execution
/// </summary>
public sealed record CommandResult
{
    public required string Output { get; init; }
    public required string Error { get; init; }
    public required int ExitCode { get; init; }
    public required string WorkingDirectory { get; init; }

    public bool IsSuccess => ExitCode == 0;

    public static CommandResult Success(string workingDirectory, string output = "") =>
        new() { Output = output, Error = "", ExitCode = 0, WorkingDirectory = workingDirectory };

    public static CommandResult Failure(string workingDirectory, string error, int exitCode = 1) =>
        new() { Output = "", Error = error, ExitCode = exitCode, WorkingDirectory = workingDirectory };
}
