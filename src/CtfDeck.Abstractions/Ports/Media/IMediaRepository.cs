using CtfDeck.Contracts.Models.Media;

namespace CtfDeck.Abstractions.Ports.Media;

public interface IMediaRepository
{
    MediaDto Create(string fileName, string mimeType, byte[] data);
    MediaDto? GetById(Guid id);
    List<MediaMetadataDto> GetAll();
    bool Delete(Guid id);
    void Insert(MediaDto media);
}
