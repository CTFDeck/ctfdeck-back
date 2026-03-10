using System.Text;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Tools;

public readonly ref struct ToolInventoryRequest
{
    public readonly Guid MessageId;

    public ToolInventoryRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId]
        MessageId = new Guid(data.Slice(1, 16));
    }
}

public class ToolInstallRequest
{
    public Guid MessageId { get; }
    public IReadOnlyList<string> ToolIds { get; }

    public ToolInstallRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][4B count][[4B len][toolId]]*
        MessageId = new Guid(data.Slice(1, 16));

        var count = BitConverter.ToInt32(data.Slice(17, 4));
        var toolIds = new List<string>(count);

        var offset = 21;
        for (var i = 0; i < count; i++)
        {
            var len = BitConverter.ToInt32(data.Slice(offset, 4));
            offset += 4;

            var toolId = Encoding.UTF8.GetString(data.Slice(offset, len));
            offset += len;

            toolIds.Add(toolId);
        }

        ToolIds = toolIds;
    }
}

public static class ToolProtocolDeserializer
{
    public static bool IsToolMessage(MessageType type)
    {
        return type >= MessageType.ToolInventoryRequest && type <= MessageType.ToolInstallProgress;
    }
}
