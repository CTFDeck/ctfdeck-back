namespace CtfDeck.Terminal.Session.Models;

public class Session
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<HistoryEntry> History { get; set; } = new();
    public List<SessionTarget> Targets { get; set; } = new();
}
