//using CtfDeck.Data.PersistenceModels.Sessions;
//
//namespace CtfDeck.Data.Repositories.Sessions;
//
//public interface ISessionRepository
//{
//    Session Create(string name);
//    Session? GetById(Guid id);
//    IEnumerable<SessionMetadata> GetAllMetadata();
//    bool Delete(Guid id);
//    bool Update(Guid id, string name, string description);
//    void AddHistoryEntry(Guid sessionId, HistoryEntry entry);
//    void UpdateTargets(Guid sessionId, List<SessionTarget> targets);
//    SessionTarget? AddTarget(Guid sessionId, SessionTarget target);
//    bool DeleteTarget(Guid sessionId, Guid targetId);
//    bool UpdateTarget(Guid sessionId, SessionTarget target);
//}
//