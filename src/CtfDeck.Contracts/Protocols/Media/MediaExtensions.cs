using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Media;

public static class MediaExtensions
{
    public static void WriteMediaMetadata(this PooledBufferWriter writer, MediaMetadataDto media)
    {
        writer.WriteGuid(media.Id);
        writer.WriteString(media.FileName);
        writer.WriteString(media.MimeType);
        writer.WriteInt64(media.CreatedAt.Ticks);
    }
}
