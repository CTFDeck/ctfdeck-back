using CtfDeck.Terminal.Session.Data;
using SessionModel = CtfDeck.Terminal.Session.Models.Session;
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

    public SessionModel Create(string name)
    {
        var session = new SessionModel
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

    public SessionModel? GetById(Guid id)
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
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    HistoryCount = s.History.Count,
                    TargetCount = s.Targets.Count
                })
                .ToList();
        }
    }

    public bool Update(Guid id, string name, string description)
    {
        lock (_lock)
        {
            var session = _context.Sessions.FindById(id);
            if (session == null) return false;

            session.Name = name;
            session.Description = description;
            session.UpdatedAt = DateTime.UtcNow;
            return _context.Sessions.Update(session);
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

    public SessionTarget? AddTarget(Guid sessionId, SessionTarget target)
    {
        lock (_lock)
        {
            var session = _context.Sessions.FindById(sessionId);
            if (session == null) return null;

            target.Id = Guid.NewGuid();
            session.Targets.Add(target);
            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
            return target;
        }
    }

    public bool DeleteTarget(Guid sessionId, Guid targetId)
    {
        lock (_lock)
        {
            var session = _context.Sessions.FindById(sessionId);
            if (session == null) return false;

            var removed = session.Targets.RemoveAll(t => t.Id == targetId);
            if (removed == 0) return false;

            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
            return true;
        }
    }

    public bool UpdateTarget(Guid sessionId, SessionTarget target)
    {
        lock (_lock)
        {
            var session = _context.Sessions.FindById(sessionId);
            if (session == null) return false;

            var existing = session.Targets.FindIndex(t => t.Id == target.Id);
            if (existing == -1) return false;

            session.Targets[existing] = target;
            session.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(session);
            return true;
        }
    }
}
