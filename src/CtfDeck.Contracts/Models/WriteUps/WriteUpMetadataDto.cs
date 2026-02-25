namespace CtfDeck.Contracts.Models.WriteUps;

public sealed class WriteUpMetadataDto
{
    public Guid Id { get; set; }
    public Guid SessionId { get; set; }
    public Guid? FolderId { get; set; }
    public string Name { get; set; } = "";
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
