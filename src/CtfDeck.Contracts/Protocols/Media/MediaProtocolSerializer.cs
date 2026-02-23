using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Media;

public static class MediaProtocolSerializer
{
    public static byte[] SerializeUploadResult(Guid messageId, bool success, Guid mediaId)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaUploadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(mediaId);
        return writer.ToArray();
    }

    public static byte[] SerializeLoadResult(Guid messageId, bool success, MediaDto? media)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaLoadResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));

        if (success && media != null)
        {
            writer.WriteGuid(media.Id);
            writer.WriteString(media.FileName);
            writer.WriteString(media.MimeType);
            writer.WriteInt32(media.Data.Length);
            writer.WriteBytes(media.Data);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.MediaDeleteResult, messageId, success);

    public static byte[] SerializeListResult(Guid messageId, List<MediaMetadataDto> mediaList)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaListResult);
        writer.WriteGuid(messageId);
        writer.WriteInt32(mediaList.Count);

        foreach (var media in mediaList)
        {
            WriteMediaMetadata(writer, media);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.MediaOperationError, messageId, error);

    private static void WriteMediaMetadata(PooledBufferWriter writer, MediaMetadataDto media)
    {
        writer.WriteGuid(media.Id);
        writer.WriteString(media.FileName);
        writer.WriteString(media.MimeType);
        writer.WriteInt64(media.CreatedAt.Ticks);
    }
}
