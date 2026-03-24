using System.Text;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.WriteUp;

public readonly ref struct WriteUpCreateRequest
{
    public readonly Guid MessageId;
    public readonly Guid? SessionId;
    public readonly Guid? SessionId;
    public readonly string Name;

    public WriteUpCreateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        var sid = reader.ReadGuid();
        SessionId = sid == Guid.Empty ? null : sid;
        Name = reader.ReadString();
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
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        WriteUpId = reader.ReadGuid();
        Name = reader.ReadString();
        Content = reader.ReadString();
    }
}

public readonly ref struct WriteUpDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;

    public WriteUpDeleteRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        WriteUpId = reader.ReadGuid();
    }
}

public readonly ref struct WriteUpListRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;
    public readonly int Offset;
    public readonly int Limit;
    public readonly bool UnassignedOnly;
    public readonly int Offset;
    public readonly int Limit;
    public readonly bool UnassignedOnly;

    public WriteUpListRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
        Offset = reader.ReadInt32();
        Limit = reader.ReadInt32();
        UnassignedOnly = reader.Remaining && reader.ReadByte() == 1;
    }
}

public readonly ref struct WriteUpLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid WriteUpId;

    public WriteUpLoadRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        WriteUpId = reader.ReadGuid();
    }
}

public static class WriteUpProtocolDeserializer
{
    public static bool IsWriteUpMessage(MessageType type)
    {
        return type >= MessageType.WriteUpCreate && type <= MessageType.WriteUpLoad;
    }
}
