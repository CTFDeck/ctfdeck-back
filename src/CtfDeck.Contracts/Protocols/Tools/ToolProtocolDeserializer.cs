using System.Text;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Tools;

public readonly ref struct ToolInventoryRequest
{
    public readonly Guid MessageId;

    public ToolInventoryRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
    }
}

public class ToolInstallRequest
{
    public Guid MessageId { get; }
    public IReadOnlyList<string> ToolIds { get; }

    public ToolInstallRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ToolIds = reader.ReadStringList();
    }
}

public class ToolUninstallRequest
{
    public Guid MessageId { get; }
    public IReadOnlyList<string> ToolIds { get; }

    public ToolUninstallRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ToolIds = reader.ReadStringList();
    }
}

public static class ToolProtocolDeserializer
{
    public static bool IsToolMessage(MessageType type)
    {
        return type >= MessageType.ToolInventoryRequest && type <= MessageType.ToolCatalogSnapshot;
    }
}
