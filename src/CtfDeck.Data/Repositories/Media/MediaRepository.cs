using CtfDeck.Abstractions.Ports.Media;
using CtfDeck.Contracts.Models.Media;
using CtfDeck.Data.Db;
using PersistenceMedia = CtfDeck.Data.PersistenceModels.Media.Media;

namespace CtfDeck.Data.Repositories.Media;

public sealed class MediaRepository : IMediaRepository
{
    private readonly CtfDeckDbContext _context;
    private readonly object _lock = new();

    public MediaRepository(CtfDeckDbContext context)
    {
        _context = context;
    }

    public MediaDto Create(string fileName, string mimeType, byte[] data)
    {
        var model = new PersistenceMedia
        {
            Id = Guid.NewGuid(),
            FileName = fileName,
            MimeType = mimeType,
            Data = data,
            CreatedAt = DateTime.UtcNow
        };

        lock (_lock)
        {
            _context.Media.Insert(model);
        }

        return ToDto(model);
    }

    public MediaDto? GetById(Guid id)
    {
        lock (_lock)
        {
            var model = _context.Media.FindById(id);
            return model == null ? null : ToDto(model);
        }
    }

    public List<MediaMetadataDto> GetAll()
    {
        lock (_lock)
        {
            return _context.Media.FindAll().Select(ToMetadataDto).ToList();
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.Media.Delete(id);
        }
    }

    private static MediaDto ToDto(PersistenceMedia m) => new()
    {
        Id = m.Id,
        FileName = m.FileName ?? "",
        MimeType = m.MimeType ?? "",
        Data = m.Data ?? [],
        CreatedAt = m.CreatedAt
    };

    private static MediaMetadataDto ToMetadataDto(PersistenceMedia m) => new()
    {
        Id = m.Id,
        FileName = m.FileName ?? "",
        MimeType = m.MimeType ?? "",
        CreatedAt = m.CreatedAt
    };
}
