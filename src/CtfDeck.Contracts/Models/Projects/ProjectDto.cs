namespace CtfDeck.Contracts.Models.Projects;

public sealed class ProjectDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProjectFolderDto> Folders { get; set; } = new();
}
