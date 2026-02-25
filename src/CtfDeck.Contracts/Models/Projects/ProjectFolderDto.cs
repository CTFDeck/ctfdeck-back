namespace CtfDeck.Contracts.Models.Projects;

public sealed class ProjectFolderDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}
