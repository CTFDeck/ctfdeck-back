namespace CtfDeck.Data.PersistenceModels.Projects;

public class ProjectFolder
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
}
