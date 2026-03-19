namespace CtfDeck.Contracts.Models.Projects;

public sealed class ProjectExportDto
{
    public int Version { get; set; } = 1;
    public DateTime ExportedAt { get; set; }
    public ProjectDto Project { get; set; } = null!;
    public List<Sessions.SessionDto> Sessions { get; set; } = new();
    public List<WriteUps.WriteUpDto> WriteUps { get; set; } = new();
    public List<MediaExportDto> Media { get; set; } = new();
}

public sealed class MediaExportDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = "";
    public string MimeType { get; set; } = "";
    public string Data { get; set; } = "";
    public DateTime CreatedAt { get; set; }
}
