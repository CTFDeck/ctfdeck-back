using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Abstractions.Ports.Projects;
using CtfDeck.Abstractions.Ports.Sessions;
using CtfDeck.Abstractions.Ports.WriteUps;

namespace CtfDeck.Terminal.Features.Projects;

public class ProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly ISessionRepository _sessionRepository;
    private readonly IWriteUpRepository _writeUpRepository;

    public ProjectService(
        IProjectRepository projectRepository,
        ISessionRepository sessionRepository,
        IWriteUpRepository writeUpRepository)
    {
        _projectRepository = projectRepository;
        _sessionRepository = sessionRepository;
        _writeUpRepository = writeUpRepository;
    }

    public ProjectDto Create(string name, string description)
        => _projectRepository.Create(name, description);

    public ProjectDto? GetById(Guid id)
        => _projectRepository.GetById(id);

    public IEnumerable<ProjectMetadataDto> GetAllMetadata()
    {
        var projects = _projectRepository.GetAllMetadata().ToList();

        foreach (var project in projects)
        {
            project.SessionCount = _sessionRepository.GetByProjectId(project.Id).Count();
        }

        return projects;
    }

    public bool Update(Guid id, string name, string description)
        => _projectRepository.Update(id, name, description);

    public bool Delete(Guid id)
    {
        var folderIds = _projectRepository.GetFolderIds(id);

        _sessionRepository.ClearProjectId(id);

        foreach (var folderId in folderIds)
        {
            _writeUpRepository.ClearFolderId(folderId);
        }

        return _projectRepository.Delete(id);
    }

    public ProjectFolderDto? AddFolder(Guid projectId, string name, Guid? parentId = null)
        => _projectRepository.AddFolder(projectId, name, parentId);

    public bool DeleteFolder(Guid projectId, Guid folderId)
    {
        _writeUpRepository.ClearFolderId(folderId);
        return _projectRepository.DeleteFolder(projectId, folderId);
    }

    public bool RenameFolder(Guid projectId, Guid folderId, string name)
        => _projectRepository.RenameFolder(projectId, folderId, name);

    public bool AssignSession(Guid sessionId, Guid projectId, Guid folderId)
    {
        var actualProjectId = projectId == Guid.Empty ? (Guid?)null : projectId;
        var actualFolderId = folderId == Guid.Empty ? (Guid?)null : folderId;
        return _sessionRepository.SetProjectAndFolderId(sessionId, actualProjectId, actualFolderId);
    }

    public bool MoveWriteUp(Guid writeUpId, Guid folderId)
    {
        var actualFolderId = folderId == Guid.Empty ? (Guid?)null : folderId;
        return _writeUpRepository.SetFolderId(writeUpId, actualFolderId);
    }

    public List<SessionMetadataDto> ListSessions(Guid projectId)
        => _sessionRepository.GetByProjectId(projectId).ToList();

    public List<WriteUpMetadataDto> ListWriteUps(Guid folderId)
        => _writeUpRepository.GetByFolderId(folderId);
}
