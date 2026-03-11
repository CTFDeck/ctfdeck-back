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
        // Format: [1B type][16B msgId][4B nameLen][name]
        MessageId = new Guid(data.Slice(1, 16));
        var nameLen = BitConverter.ToInt32(data.Slice(17, 4));
        Name = Encoding.UTF8.GetString(data.Slice(21, nameLen));
    }
}

public readonly ref struct SessionSetActiveRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;

    public SessionSetActiveRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct SessionLoadRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;

    public SessionLoadRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct SessionListRequest
{
    public readonly Guid MessageId;
    public readonly int Offset;
    public readonly int Limit;

    public SessionListRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][4B offset][4B limit]
        MessageId = new Guid(data.Slice(1, 16));
        Offset = BitConverter.ToInt32(data.Slice(17, 4));
        Limit = BitConverter.ToInt32(data.Slice(21, 4));
    }
}

public readonly ref struct SessionDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid SessionId;

    public SessionDeleteRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));
    }
}

public class SessionUpdateTargetsRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public List<SessionTargetDto> Targets { get; }

    public SessionUpdateTargetsRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][4B count][...targets]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));

        var count = BitConverter.ToInt32(data.Slice(33, 4));
        Targets = new List<SessionTargetDto>(count);

        var offset = 37;
        for (var i = 0; i < count; i++)
        {
            var id = new Guid(data.Slice(offset, 16));
            offset += 16;
            Targets.Add(TargetBinaryReader.ReadFields(data, ref offset, id));
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
        // Format: [1B type][16B msgId][16B sessionId][4B nameLen][name][4B descLen][desc]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));

        var offset = 33;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        var descLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Description = Encoding.UTF8.GetString(data.Slice(offset, descLen));
    }
}

public class SessionAddTargetRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public SessionTargetDto Target { get; }

    public SessionAddTargetRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][target fields...]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));

        var offset = 33;
        Target = TargetBinaryReader.ReadFields(data, ref offset);
    }
}

public class SessionDeleteTargetRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public Guid TargetId { get; }

    public SessionDeleteTargetRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][16B targetId]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));
        TargetId = new Guid(data.Slice(33, 16));
    }
}

public class SessionEditTargetRequest
{
    public Guid MessageId { get; }
    public Guid SessionId { get; }
    public SessionTargetDto Target { get; }

    public SessionEditTargetRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][16B targetId][target fields...]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));
        var targetId = new Guid(data.Slice(33, 16));

        var offset = 49;
        Target = TargetBinaryReader.ReadFields(data, ref offset, targetId);
    }
}

public static class SessionProtocolDeserializer
{
    public static bool IsSessionMessage(MessageType type)
    {
        return type >= MessageType.SessionCreate && type <= MessageType.SessionEditTarget;
    }
}
