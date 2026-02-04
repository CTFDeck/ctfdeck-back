using CtfDeck.Terminal.Session.Models;

namespace CtfDeck.Terminal.Session.Repositories;

public interface ISessionRepository
{
    Models.Session Create(string name);
    Models.Session? GetById(Guid id);
    IEnumerable<SessionMetadata> GetAllMetadata();
    bool Delete(Guid id);
    void AddHistoryEntry(Guid sessionId, HistoryEntry entry);
    void UpdateTargets(Guid sessionId, List<SessionTarget> targets);
}
