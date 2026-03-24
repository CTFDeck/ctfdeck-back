using System.Text;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.WriteUp;

public readonly ref struct WriteUpCreateRequest
{
    public readonly Guid MessageId;
    public readonly Guid? SessionId;
    public readonly string Name;

    public WriteUpCreateRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][4B nameLen][name]
        MessageId = new Guid(data.Slice(1, 16));
        var sid = new Guid(data.Slice(17, 16));
        SessionId = sid == Guid.Empty ? null : sid;

        var offset = 33;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
    }
}

public readonly ref struct WriteUpUpdateRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;
    public readonly string Name;
    public readonly string Content;

    public WriteUpUpdateRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B writeUpId][4B nameLen][name][4B contentLen][content]
        MessageId = new Guid(data.Slice(1, 16));
        WriteUpId = new Guid(data.Slice(17, 16));

        var offset = 33;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        var contentLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Content = Encoding.UTF8.GetString(data.Slice(offset, contentLen));
    }
}

public readonly ref struct WriteUpDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;

    public WriteUpDeleteRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B writeUpId]
        MessageId = new Guid(data.Slice(1, 16));
        WriteUpId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct WriteUpListRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;
    public readonly int Offset;
    public readonly int Limit;
    public readonly bool UnassignedOnly;

    public WriteUpListRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][4B offset][4B limit][1B unassignedOnly]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));
        Offset = BitConverter.ToInt32(data.Slice(33, 4));
        Limit = BitConverter.ToInt32(data.Slice(37, 4));
        UnassignedOnly = data.Length > 41 && data[41] == 1;
    }
}

public readonly ref struct WriteUpLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;

    public WriteUpLoadRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B writeUpId]
        MessageId = new Guid(data.Slice(1, 16));
        WriteUpId = new Guid(data.Slice(17, 16));
    }
}

public static class WriteUpProtocolDeserializer
{
    public static bool IsWriteUpMessage(MessageType type)
    {
        return type >= MessageType.WriteUpCreate && type <= MessageType.WriteUpLoad;
    }
}
