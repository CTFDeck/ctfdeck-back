using CtfDeck.Abstractions.Ports.WriteUps;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Data.Db;
using CtfDeck.Data.PersistenceModels.WriteUps;

namespace CtfDeck.Data.Repositories.WriteUps;

public sealed class WriteUpRepository : IWriteUpRepository
{
    private readonly CtfDeckDbContext _context;
    private readonly object _lock = new();

    public WriteUpRepository(CtfDeckDbContext context)
    {
        _context = context;
    }

    public WriteUpDto Create(Guid? sessionId, string name)
    {
        var now = DateTime.UtcNow;
        var model = new WriteUp
        {
            Id = Guid.NewGuid(),
            SessionId = sessionId,
            Name = name,
            Content = "",
            CreatedAt = now,
            UpdatedAt = now
        };

        lock (_lock)
        {
            _context.WriteUps.Insert(model);
        }

        return ToDto(model);
    }

    public WriteUpDto? GetById(Guid id)
    {
        lock (_lock)
        {
            var model = _context.WriteUps.FindById(id);
            return model == null ? null : ToDto(model);
        }
    }

    public (IEnumerable<WriteUpMetadataDto> Items, int TotalCount) GetBySessionId(Guid sessionId, int offset = 0, int limit = 50)
    {
        lock (_lock)
        {
            var query = _context.WriteUps.Find(w => w.SessionId == sessionId);
            var totalCount = query.Count();

            var items = query
                .OrderByDescending(w => w.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(ToMetadataDto)
                .ToList();
                
            return (items, totalCount);
        }
    }

    public bool Update(Guid id, string name, string content)
    {
        lock (_lock)
        {
            var model = _context.WriteUps.FindById(id);
            if (model == null) return false;

            model.Name = name;
            model.Content = content;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.WriteUps.Update(model);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.WriteUps.Delete(id);
        }
    }

    public (IEnumerable<WriteUpMetadataDto> Items, int TotalCount) GetByFolderId(Guid folderId, int offset = 0, int limit = 50)
    {
        lock (_lock)
        {
            var query = _context.WriteUps.Find(w => w.FolderId == folderId);
            var totalCount = query.Count();

            var items = query
                .OrderByDescending(w => w.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(ToMetadataDto)
                .ToList();
                
            return (items, totalCount);
        }
    }

    public bool SetFolderId(Guid writeUpId, Guid? folderId)
    {
        lock (_lock)
        {
            var model = _context.WriteUps.FindById(writeUpId);
            if (model == null) return false;

            model.FolderId = folderId;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.WriteUps.Update(model);
        }
    }

    public bool SetProjectAndFolderId(Guid writeUpId, Guid? projectId, Guid? folderId)
    {
        lock (_lock)
        {
            var model = _context.WriteUps.FindById(writeUpId);
            if (model == null) return false;

            model.ProjectId = projectId;
            model.FolderId = folderId;
            model.UpdatedAt = DateTime.UtcNow;

            return _context.WriteUps.Update(model);
        }
    }

    public void ClearFolderId(Guid folderId)
    {
        lock (_lock)
        {
            var writeUps = _context.WriteUps.Find(w => w.FolderId == folderId).ToList();
            foreach (var writeUp in writeUps)
            {
                writeUp.FolderId = null;
                _context.WriteUps.Update(writeUp);
            }
        }
    }

    public void ClearProjectId(Guid projectId)
    {
        lock (_lock)
        {
            var writeUps = _context.WriteUps.Find(w => w.ProjectId == projectId).ToList();
            foreach (var writeUp in writeUps)
            {
                writeUp.ProjectId = null;
                writeUp.FolderId = null;
                _context.WriteUps.Update(writeUp);
            }
        }
    }

    public (IEnumerable<WriteUpMetadataDto> Items, int TotalCount) GetAllMetadata(int offset = 0, int limit = 50, bool unassignedOnly = false)
    {
        lock (_lock)
        {
            var query = unassignedOnly 
                ? _context.WriteUps.Find(w => w.ProjectId == null)
                : _context.WriteUps.FindAll();
                
            var totalCount = query.Count();

            var items = query
                .OrderByDescending(w => w.CreatedAt)
                .Skip(offset)
                .Take(limit)
                .Select(ToMetadataDto)
                .ToList();
                
            return (items, totalCount);
        }
    }

    private static WriteUpDto ToDto(WriteUp m) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        ProjectId = m.ProjectId,
        FolderId = m.FolderId,
        Name = m.Name ?? "",
        Content = m.Content ?? "",
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    private static WriteUpMetadataDto ToMetadataDto(WriteUp m) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        ProjectId = m.ProjectId,
        FolderId = m.FolderId,
        Name = m.Name ?? "",
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
}
