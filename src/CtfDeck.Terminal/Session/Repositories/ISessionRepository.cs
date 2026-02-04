using SessionModel = CtfDeck.Terminal.Session.Models.Session;
using CtfDeck.Terminal.Session.Models;

namespace CtfDeck.Terminal.Session.Repositories;

public interface ISessionRepository
{
    SessionModel Create(string name);
    SessionModel? GetById(Guid id);
    IEnumerable<SessionMetadata> GetAllMetadata();
    bool Delete(Guid id);
    void AddHistoryEntry(Guid sessionId, HistoryEntry entry);
    void UpdateTargets(Guid sessionId, List<SessionTarget> targets);
}
