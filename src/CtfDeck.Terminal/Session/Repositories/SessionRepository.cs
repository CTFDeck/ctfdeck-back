using CtfDeck.Terminal.Session.Data;
using CtfDeck.Terminal.Session.Models;

namespace CtfDeck.Terminal.Session.Repositories;

public class SessionRepository : ISessionRepository
{
    private readonly SessionDbContext _context;
    private readonly object _lock = new();

    public SessionRepository(SessionDbContext context)
    {
        _context = context;
    }

    public Models.Session Create(string name)
    {
        var session = new Models.Session
        {
            Id = Guid.NewGuid(),
            Name = name,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            History = new List<HistoryEntry>(),
            Targets = new List<SessionTarget>()
        };

        lock (_lock)
        {
            _context.Sessions.Insert(session);
        }

        return session;
    }

    public Models.Session? GetById(Guid id)
    {
        lock (_lock)
        {
            return _context.Sessions.FindById(id);
        }
    }

    public IEnumerable<SessionMetadata> GetAllMetadata()
    {
        lock (_lock)
        {
            return _context.Sessions
                .FindAll()
                .Select(s => new SessionMetadata
                {
                    Id = s.Id,
                    Name = s.Name,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    HistoryCount = s.History.Count,
                    TargetCount = s.Targets.Count
                })
                .ToList();
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.Sessions.Delete(id);
        }
    }

    public void AddHistoryEntry(Guid sessionId, HistoryEntry entry)
    {
        lock (_lock)
        {
            var session = _context.Sessions.FindById(sessionId);
            if (session == null) return;

            entry.Id = Guid.NewGuid();
            session.History.Add(entry);
            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
        }
    }

    public void UpdateTargets(Guid sessionId, List<SessionTarget> targets)
    {
        lock (_lock)
        {
            var session = _context.Sessions.FindById(sessionId);
            if (session == null) return;

            session.Targets = targets;
            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
        }
    }
}
