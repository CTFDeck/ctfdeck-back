using CtfDeck.Contracts.Models.Sessions;

namespace CtfDeck.Abstractions.Ports.Sessions;

public interface ISessionRepository
{
    SessionDto Create(string name);
    SessionDto? GetById(Guid id);
    IEnumerable<SessionMetadataDto> GetAllMetadata();
    bool Update(Guid id, string name, string description);
    bool Delete(Guid id);

    void AddHistoryEntry(Guid sessionId, HistoryEntryDto entry);

    void UpdateTargets(Guid sessionId, List<SessionTargetDto> targets);
    SessionTargetDto? AddTarget(Guid sessionId, SessionTargetDto target);
    bool DeleteTarget(Guid sessionId, Guid targetId);
    bool UpdateTarget(Guid sessionId, SessionTargetDto target);
}
