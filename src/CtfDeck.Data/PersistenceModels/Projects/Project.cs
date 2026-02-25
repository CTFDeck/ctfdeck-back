namespace CtfDeck.Data.PersistenceModels.Projects;

public class Project
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ProjectFolder> Folders { get; set; } = new();
}
