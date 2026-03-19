namespace CtfDeck.Contracts.Models.Sessions;

public sealed class SessionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? FolderId { get; set; }
    public List<HistoryEntryDto> History { get; set; } = new();
    public List<SessionTargetDto> Targets { get; set; } = new();
}
