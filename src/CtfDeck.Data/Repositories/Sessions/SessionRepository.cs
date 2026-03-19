using CtfDeck.Abstractions.Ports.Sessions;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Data.Db;
using CtfDeck.Data.PersistenceModels.Sessions;

namespace CtfDeck.Data.Repositories.Sessions;

public sealed class SessionRepository : ISessionRepository
{
    private readonly CtfDeckDbContext _context;
    private readonly object _lock = new();

    public SessionRepository(CtfDeckDbContext context)
    {
        _context = context;
    }

    public SessionDto Create(string name)
    {
        var model = new Session
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
            _context.Sessions.Insert(model);
        }

        return ToDto(model);
    }

    public SessionDto? GetById(Guid id)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(id);
            return model == null ? null : ToDto(model);
        }
    }

    public (IEnumerable<SessionMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50, bool unassignedOnly = false)
    {
        lock (_lock)
        {
            var query = unassignedOnly
                ? _context.Sessions.Find(s => s.ProjectId == null)
                : _context.Sessions.FindAll();

            var totalCount = query.Count();

            var items = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(s => new SessionMetadataDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    HistoryCount = s.History?.Count ?? 0,
                    TargetCount = s.Targets?.Count ?? 0,
                    ProjectId = s.ProjectId,
                    FolderId = s.FolderId
                })
                .ToList();

            return (items, totalCount);
        }
    }

    public bool Update(Guid id, string name, string description)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(id);
            if (model == null) return false;

            model.Name = name;
            model.Description = description;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Sessions.Update(model);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.Sessions.Delete(id);
        }
    }

    public void AddHistoryEntry(Guid sessionId, HistoryEntryDto entry)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return;

            var history = new HistoryEntry
            {
                Id = entry.Id == Guid.Empty ? Guid.NewGuid() : entry.Id,
                Timestamp = entry.Timestamp,
                WorkingDirectory = entry.WorkingDirectory,
                Command = entry.Command,
                Output = entry.Output,
                ExitCode = entry.ExitCode
            };

            model.History ??= new List<HistoryEntry>();
            model.History.Add(history);

            model.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(model);
        }
    }

    public void UpdateTargets(Guid sessionId, List<SessionTargetDto> targets)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return;

            model.Targets = targets.Select(ToPersistence).ToList();
            model.UpdatedAt = DateTime.UtcNow;

            _context.Sessions.Update(model);
        }
    }

    public SessionTargetDto? AddTarget(Guid sessionId, SessionTargetDto target)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return null;

            if (target.Id == Guid.Empty)
                target.Id = Guid.NewGuid();

            model.Targets ??= new List<SessionTarget>();
            model.Targets.Add(ToPersistence(target));

            model.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(model);

            return target;
        }
    }

    public bool DeleteTarget(Guid sessionId, Guid targetId)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return false;

            model.Targets ??= new List<SessionTarget>();
            var removed = model.Targets.RemoveAll(t => t.Id == targetId);
            if (removed == 0) return false;

            model.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(model);

            return true;
        }
    }

    public bool UpdateTarget(Guid sessionId, SessionTargetDto target)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return false;

            model.Targets ??= new List<SessionTarget>();
            var index = model.Targets.FindIndex(t => t.Id == target.Id);
            if (index == -1) return false;

            model.Targets[index] = ToPersistence(target);

            model.UpdatedAt = DateTime.UtcNow;
            _context.Sessions.Update(model);

            return true;
        }
    }

    public (IEnumerable<SessionMetadataDto> Items, int TotalCount) GetByProjectId(Guid projectId, int offset = 0, int limit = 50)
    {
        lock (_lock)
        {
            var query = _context.Sessions.Find(s => s.ProjectId == projectId);
            var totalCount = query.Count();

            var items = query
                .OrderByDescending(s => s.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(s => new SessionMetadataDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    HistoryCount = s.History?.Count ?? 0,
                    TargetCount = s.Targets?.Count ?? 0,
                    ProjectId = s.ProjectId,
                    FolderId = s.FolderId
                })
                .ToList();

            return (items, totalCount);
        }
    }

    public bool SetProjectId(Guid sessionId, Guid? projectId)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return false;

            model.ProjectId = projectId;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Sessions.Update(model);
        }
    }

    public bool SetFolderId(Guid sessionId, Guid? folderId)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return false;

            model.FolderId = folderId;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Sessions.Update(model);
        }
    }

    public bool SetProjectAndFolderId(Guid sessionId, Guid? projectId, Guid? folderId)
    {
        lock (_lock)
        {
            var model = _context.Sessions.FindById(sessionId);
            if (model == null) return false;

            model.ProjectId = projectId;
            model.FolderId = folderId;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.Sessions.Update(model);
        }
    }

    public IEnumerable<SessionMetadataDto> GetByFolderId(Guid folderId)
    {
        lock (_lock)
        {
            var query = _context.Sessions.Find(s => s.FolderId == folderId);

            var items = query
                .Select(s => new SessionMetadataDto
                {
                    Id = s.Id,
                    Name = s.Name,
                    Description = s.Description,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    HistoryCount = s.History?.Count ?? 0,
                    TargetCount = s.Targets?.Count ?? 0,
                    ProjectId = s.ProjectId,
                    FolderId = s.FolderId
                })
                .ToList();

            return items;
        }
    }

    public void ClearProjectId(Guid projectId)
    {
        lock (_lock)
        {
            var sessions = _context.Sessions.Find(s => s.ProjectId == projectId).ToList();
            foreach (var session in sessions)
            {
                session.ProjectId = null;
                _context.Sessions.Update(session);
            }
        }
    }

    public void Insert(SessionDto session)
    {
        var model = new Session
        {
            Id = session.Id,
            Name = session.Name,
            Description = session.Description,
            CreatedAt = session.CreatedAt,
            UpdatedAt = session.UpdatedAt,
            ProjectId = session.ProjectId,
            FolderId = session.FolderId,
            History = session.History.Select(h => new HistoryEntry
            {
                Id = h.Id,
                Timestamp = h.Timestamp,
                WorkingDirectory = h.WorkingDirectory,
                Command = h.Command,
                Output = h.Output,
                ExitCode = h.ExitCode
            }).ToList(),
            Targets = session.Targets.Select(ToPersistence).ToList()
        };

        lock (_lock)
        {
            _context.Sessions.Insert(model);
        }
    }

    private static SessionDto ToDto(Session s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        Description = s.Description,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt,
        ProjectId = s.ProjectId,
        FolderId = s.FolderId,
        History = (s.History ?? new List<HistoryEntry>()).Select(h => new HistoryEntryDto
        {
            Id = h.Id,
            Timestamp = h.Timestamp,
            WorkingDirectory = h.WorkingDirectory,
            Command = h.Command,
            Output = h.Output,
            ExitCode = h.ExitCode
        }).ToList(),
        Targets = (s.Targets ?? new List<SessionTarget>()).Select(t => new SessionTargetDto
        {
            Id = t.Id,
            Name = t.Name,
            Address = t.Address,
            Port = t.Port,
            Description = t.Description,
            Type = (Contracts.Models.Sessions.TargetType)t.Type
        }).ToList()
    };

    private static SessionTarget ToPersistence(SessionTargetDto t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Address = t.Address,
        Port = t.Port,
        Description = t.Description,
        Type = (CtfDeck.Data.PersistenceModels.Sessions.TargetType)t.Type
    };
}
