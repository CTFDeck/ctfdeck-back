using CtfDeck.Contracts.Models.Projects;

namespace CtfDeck.Abstractions.Ports.Projects;

public interface IProjectRepository
{
    ProjectDto Create(string name, string description);
    ProjectDto? GetById(Guid id);
    IEnumerable<ProjectMetadataDto> GetAllMetadata();
    bool Update(Guid id, string name, string description);
    bool Delete(Guid id);
    ProjectFolderDto? AddFolder(Guid projectId, string name, Guid? parentId = null);
    bool DeleteFolder(Guid projectId, Guid folderId);
    bool RenameFolder(Guid projectId, Guid folderId, string name);
    List<Guid> GetFolderIds(Guid projectId);
}
