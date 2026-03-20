using System.Text;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Session;

public readonly ref struct SessionCreateRequest
{
    public readonly Guid MessageId;
    public readonly string Name;

    public SessionCreateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Name = reader.ReadString();
    }
}

public readonly ref struct SessionSetActiveRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;

    public SessionSetActiveRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
    }
}

public readonly ref struct SessionLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;

    public SessionLoadRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
    }
}

public readonly ref struct SessionListRequest
{
    public readonly Guid MessageId;
    public readonly int Offset;
    public readonly int Limit;
    public readonly bool UnassignedOnly;

    public SessionListRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Offset = reader.ReadInt32();
        Limit = reader.ReadInt32();
        UnassignedOnly = reader.Remaining && reader.ReadByte() == 1;
    }
}

public readonly ref struct SessionDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;

    public SessionDeleteRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
    }
}

public class SessionUpdateTargetsRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public List<SessionTargetDto> Targets { get; }

    public SessionUpdateTargetsRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();

        var count = reader.ReadInt32();
        Targets = new List<SessionTargetDto>(count);

        for (var i = 0; i < count; i++)
        {
            Targets.Add(reader.ReadTarget());
        }
    }
}

public class SessionUpdateRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public string Name { get; }
    public string Description { get; }

    public SessionUpdateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
        Name = reader.ReadString();
        Description = reader.ReadString();
    }
}

public class SessionAddTargetRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public SessionTargetDto Target { get; }

    public SessionAddTargetRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();

        Target = reader.ReadTarget();
    }
}

public class SessionDeleteTargetRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public Guid TargetId { get; }

    public SessionDeleteTargetRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
        TargetId = reader.ReadGuid();
    }
}

public class SessionEditTargetRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public SessionTargetDto Target { get; }

    public SessionEditTargetRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        SessionId = reader.ReadGuid();
        Target = reader.ReadTarget();
    }
}

public static class SessionProtocolDeserializer
{
    public static bool IsSessionMessage(MessageType type)
    {
        return type >= MessageType.SessionCreate && type <= MessageType.SessionEditTarget;
    }
}
