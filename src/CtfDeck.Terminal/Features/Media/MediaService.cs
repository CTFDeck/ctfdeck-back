using CtfDeck.Contracts.Models.Media;
using CtfDeck.Abstractions.Ports.Media;

namespace CtfDeck.Terminal.Features.Media;

public class MediaService
{
    private readonly IMediaRepository _repository;

    public MediaService(IMediaRepository repository)
    {
        _repository = repository;
    }

    public MediaDto Create(string fileName, string mimeType, byte[] data)
        => _repository.Create(fileName, mimeType, data);

    public MediaDto? GetById(Guid id)
        => _repository.GetById(id);

    public List<MediaMetadataDto> GetAll()
        => _repository.GetAll();

    public bool Delete(Guid id)
        => _repository.Delete(id);
}
