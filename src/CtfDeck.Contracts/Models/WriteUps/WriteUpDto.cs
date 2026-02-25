namespace CtfDeck.Contracts.Models.WriteUps;

public sealed class WriteUpDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? FolderId { get; set; }
    public string Name { get; set; } = "";
    public string Content { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
