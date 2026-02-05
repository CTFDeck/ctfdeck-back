using System.Text;
using CtfDeck.Terminal.Session.Models;
using CtfDeck.Terminal.WebSocket;

namespace CtfDeck.Terminal.Session.Protocol;

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

    public SessionListRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId]
        MessageId = new Guid(data.Slice(1, 16));
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
    public List<SessionTarget> Targets { get; }

    public SessionUpdateTargetsRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B sessionId][4B count][...targets]
        MessageId = new Guid(data.Slice(1, 16));
        SessionId = new Guid(data.Slice(17, 16));

        var count = BitConverter.ToInt32(data.Slice(33, 4));
        Targets = new List<SessionTarget>(count);

        var offset = 37;
        for (var i = 0; i < count; i++)
        {
            var target = ReadTarget(data, ref offset);
            Targets.Add(target);
        }
    }

    private static SessionTarget ReadTarget(ReadOnlySpan<byte> data, ref int offset)
    {
        var id = new Guid(data.Slice(offset, 16));
        offset += 16;

        var addrLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        var address = Encoding.UTF8.GetString(data.Slice(offset, addrLen));
        offset += addrLen;

        var portValue = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        int? port = portValue == -1 ? null : portValue;

        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        var name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        var type = (TargetType)BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        return new SessionTarget
        {
            Id = id,
            Address = address,
            Port = port,
            Name = name,
            Type = type
        };
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

public static class SessionProtocolDeserializer
{
    public static bool IsSessionMessage(MessageType type)
    {
        return type >= MessageType.SessionCreate && type <= MessageType.SessionUpdate;
    }
}
