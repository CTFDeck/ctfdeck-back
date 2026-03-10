using CtfDeck.Contracts.Models.Tools;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Tools;

public static class ToolProtocolSerializer
{
    public static byte[] SerializeInventoryResult(Guid messageId, IEnumerable<ToolStatusDto> tools)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInventoryResult);
        writer.WriteGuid(messageId);

        var list = tools.ToList();
        writer.WriteInt32(list.Count);

        foreach (var tool in list)
        {
            writer.WriteString(tool.Id);
            writer.WriteString(tool.DisplayName);
            writer.WriteString(tool.Description);
            writer.WriteString(tool.Kind);
            writer.WriteByte((byte)(tool.IsInstalled ? 1 : 0));
            writer.WriteByte((byte)(tool.IsInstallable ? 1 : 0));
            writer.WriteString(tool.InstalledPath ?? string.Empty);
            writer.WriteString(tool.Version ?? string.Empty);
            writer.WriteString(tool.Reason ?? string.Empty);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeInstallAccepted(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.ToolInstallAccepted, messageId, success);

    public static byte[] SerializeInstallProgress(Guid messageId, ToolInstallProgressDto progress)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInstallProgress);
        writer.WriteGuid(messageId);

        writer.WriteString(progress.ToolId);
        writer.WriteInt32((int)progress.State);
        writer.WriteString(progress.Message ?? string.Empty);

        writer.WriteByte((byte)(progress.ProgressPercent.HasValue ? 1 : 0));
        if (progress.ProgressPercent.HasValue)
            writer.WriteInt32(progress.ProgressPercent.HasValue ? (int)(progress.ProgressPercent.Value * 100) : -1);

        writer.WriteString(progress.InstalledPath ?? string.Empty);
        writer.WriteString(progress.Error ?? string.Empty);

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.ToolOperationError, messageId, error);

    public static byte[] SerializeCatalogSnapshot(IEnumerable<ToolCatalogItemDto> tools)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolCatalogSnapshot);

        var list = tools.ToList();
        writer.WriteInt32(list.Count);

        foreach (var tool in list)
        {
            writer.WriteString(tool.Id);
            writer.WriteString(tool.DisplayName);
            writer.WriteString(tool.Category);
            writer.WriteString(tool.Kind);
            writer.WriteString(tool.Description);
            writer.WriteString(tool.ExternalUrl ?? string.Empty);
            writer.WriteByte((byte)(tool.IsInstalled ? 1 : 0));
            writer.WriteByte((byte)(tool.IsInstallable ? 1 : 0));
            writer.WriteString(tool.InstalledPath ?? string.Empty);
            writer.WriteString(tool.Version ?? string.Empty);
            writer.WriteString(tool.Reason ?? string.Empty);
        }

        return writer.ToArray();
    }
}
