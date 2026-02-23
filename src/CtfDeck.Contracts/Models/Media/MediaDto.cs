namespace CtfDeck.Contracts.Models.Media;

public sealed class MediaDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public byte[] Data { get; set; } = [];
    public DateTime CreatedAt { get; set; }
}
