using CtfDeck.Contracts.Models.Tools;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Tools;

public static class ToolProtocolSerializer
{
    public static byte[] SerializeInventoryResult(Guid messageId, IEnumerable<ToolStatusDto> tools)
        => BinaryProtocolSerializer.SerializeList(MessageType.ToolInventoryResult, messageId, tools, (w, t) => w.WriteToolStatus(t));

    public static byte[] SerializeInstallAccepted(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ToolInstallAccepted, messageId, success);

    public static byte[] SerializeInstallProgress(Guid messageId, ToolInstallProgressDto progress)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInstallProgress);
        writer.WriteGuid(messageId);
        writer.WriteToolInstallProgress(progress);
        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.ToolOperationError, messageId, error);

    public static byte[] SerializeCatalogSnapshot(IEnumerable<ToolCatalogItemDto> tools)
        => BinaryProtocolSerializer.SerializeList(MessageType.ToolCatalogSnapshot, tools, (w, t) => w.WriteToolCatalogItem(t));
}
