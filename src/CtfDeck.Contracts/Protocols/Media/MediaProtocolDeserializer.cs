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
        // Format: [1B type][16B msgId][4B fileNameLen][fileName][4B mimeTypeLen][mimeType][4B dataLen][data]
        MessageId = new Guid(data.Slice(1, 16));

        var offset = 17;
        var fileNameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        FileName = Encoding.UTF8.GetString(data.Slice(offset, fileNameLen));
        offset += fileNameLen;

        var mimeTypeLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        MimeType = Encoding.UTF8.GetString(data.Slice(offset, mimeTypeLen));
        offset += mimeTypeLen;

        var dataLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Data = data.Slice(offset, dataLen);
    }
}

public readonly ref struct MediaLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid MediaId;

    public MediaLoadRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B mediaId]
        MessageId = new Guid(data.Slice(1, 16));
        MediaId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct MediaDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid MediaId;

    public MediaDeleteRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B mediaId]
        MessageId = new Guid(data.Slice(1, 16));
        MediaId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct MediaListRequest
{
    public readonly Guid MessageId;

    public MediaListRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId]
        MessageId = new Guid(data.Slice(1, 16));
    }
}

public static class MediaProtocolDeserializer
{
    public static bool IsMediaMessage(MessageType type)
    {
        return type >= MessageType.MediaUpload && type <= MessageType.MediaList;
    }
}
