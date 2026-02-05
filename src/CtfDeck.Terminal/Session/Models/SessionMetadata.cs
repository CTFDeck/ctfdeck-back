namespace CtfDeck.Terminal.Session.Models;

public class SessionMetadata
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int HistoryCount { get; set; }
    public int TargetCount { get; set; }
}
