namespace CtfDeck.Terminal.Terminal;

public class CommandResult
{
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public int ExitCode { get; set; }
    public string WorkingDirectory { get; set; } = "";
}
