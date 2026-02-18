namespace CtfDeck.Contracts.Models.Sessions;

public sealed class HistoryEntryDto
{
    public Guid Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string WorkingDirectory { get; set; } = string.Empty;
    public string Command { get; set; } = string.Empty;
    public string Output { get; set; } = string.Empty;
    public int ExitCode { get; set; }
}
