namespace CtfDeck.Contracts.Models.Projects;

public sealed class ProjectMetadataDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int FolderCount { get; set; }
    public int SessionCount { get; set; }
}
