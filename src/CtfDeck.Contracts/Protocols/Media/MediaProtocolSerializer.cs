using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Media;

public static class MediaProtocolSerializer
{
    public static byte[] SerializeUploadResult(Guid messageId, bool success, Guid mediaId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.MediaUploadResult, messageId, success, mediaId);

    public static byte[] SerializeLoadResult(Guid messageId, bool success, Guid mediaId, byte[]? data)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte(success ? (byte)1 : (byte)0);

        if (success)
        {
            writer.WriteGuid(mediaId);
            writer.WriteInt32(data?.Length ?? 0);
            if (data != null)
            {
                writer.WriteBytes(data);
            }
        }

        return writer.ToArray();
    }

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.MediaDeleteResult, messageId, success);

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<MediaMetadataDto> media)
        => BinaryProtocolSerializer.SerializeList(MessageType.MediaListResult, messageId, media, (w, m) => w.WriteMediaMetadata(m));

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.MediaOperationError, messageId, error);
}
