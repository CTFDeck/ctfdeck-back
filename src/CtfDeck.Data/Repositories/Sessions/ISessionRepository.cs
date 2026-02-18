using SessionModel = CtfDeck.Terminal.Session.Models.Session;
using CtfDeck.Terminal.Session.Models;

namespace CtfDeck.Data.Repositories.Sessions;

public interface ISessionRepository
{
    SessionModel Create(string name);
    SessionModel? GetById(Guid id);
    IEnumerable<SessionMetadata> GetAllMetadata();
    bool Delete(Guid id);
    bool Update(Guid id, string name, string description);
    void AddHistoryEntry(Guid sessionId, HistoryEntry entry);
    void UpdateTargets(Guid sessionId, List<SessionTarget> targets);
    SessionTarget? AddTarget(Guid sessionId, SessionTarget target);
    bool DeleteTarget(Guid sessionId, Guid targetId);
    bool UpdateTarget(Guid sessionId, SessionTarget target);
}
