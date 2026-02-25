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

    public WriteUpDto Create(Guid sessionId, string name)
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

    public List<WriteUpMetadataDto> GetBySessionId(Guid sessionId)
    {
        lock (_lock)
        {
            return _context.WriteUps
                .Find(w => w.SessionId == sessionId)
                .Select(ToMetadataDto)
                .ToList();
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

    private static WriteUpDto ToDto(WriteUp m) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        Name = m.Name ?? "",
        Content = m.Content ?? "",
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };

    private static WriteUpMetadataDto ToMetadataDto(WriteUp m) => new()
    {
        Id = m.Id,
        SessionId = m.SessionId,
        Name = m.Name ?? "",
        CreatedAt = m.CreatedAt,
        UpdatedAt = m.UpdatedAt
    };
}
