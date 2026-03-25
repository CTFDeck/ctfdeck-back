using System.Text;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Media;

public readonly ref struct MediaUploadRequest
{
    public readonly Guid MessageId;
    public readonly string FileName;
    public readonly string MimeType;
    public readonly ReadOnlySpan<byte> Data;

    public MediaUploadRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        FileName = reader.ReadString();
        MimeType = reader.ReadString();
        var dataLen = reader.ReadInt32();
        Data = reader.ReadBytes(dataLen);
    }
}

public readonly ref struct MediaLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid MediaId;

    public MediaLoadRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        MediaId = reader.ReadGuid();
    }
}

public readonly ref struct MediaDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid MediaId;

    public MediaDeleteRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        MediaId = reader.ReadGuid();
    }
}

public readonly ref struct MediaListRequest
{
    public readonly Guid MessageId;

    public MediaListRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
    }
}

public static class MediaProtocolDeserializer
{
    public static bool IsMediaMessage(MessageType type)
    {
        return type >= MessageType.MediaUpload && type <= MessageType.MediaList;
    }
}
