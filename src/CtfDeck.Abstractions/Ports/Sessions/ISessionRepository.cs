using CtfDeck.Contracts.Models.Sessions;

namespace CtfDeck.Abstractions.Ports.Sessions;

public interface ISessionRepository
{
    SessionDto Create(string name);
    SessionDto? GetById(Guid id);
    (IEnumerable<SessionMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50, bool unassignedOnly = false);
    bool Update(Guid id, string name, string description);
    bool Delete(Guid id);

    void AddHistoryEntry(Guid sessionId, HistoryEntryDto entry);

    void UpdateTargets(Guid sessionId, List<SessionTargetDto> targets);
    SessionTargetDto? AddTarget(Guid sessionId, SessionTargetDto target);
    bool DeleteTarget(Guid sessionId, Guid targetId);
    bool UpdateTarget(Guid sessionId, SessionTargetDto target);

    (IEnumerable<SessionMetadataDto> Items, int TotalCount) GetByProjectId(Guid projectId, int offset = 0, int limit = 50);
    IEnumerable<SessionMetadataDto> GetByFolderId(Guid folderId);
    bool SetProjectId(Guid sessionId, Guid? projectId);
    bool SetFolderId(Guid sessionId, Guid? folderId);
    bool SetProjectAndFolderId(Guid sessionId, Guid? projectId, Guid? folderId);
    void ClearProjectId(Guid projectId);
}
