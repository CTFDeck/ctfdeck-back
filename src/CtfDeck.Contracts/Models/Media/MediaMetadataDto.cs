namespace CtfDeck.Contracts.Models.Media;

public sealed class MediaMetadataDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
