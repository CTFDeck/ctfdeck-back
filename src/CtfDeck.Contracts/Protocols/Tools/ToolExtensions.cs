using CtfDeck.Contracts.Models.Tools;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.Tools;

public static class ToolExtensions
{
    public static void WriteToolStatus(this PooledBufferWriter writer, ToolStatusDto tool)
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

    public static void WriteToolInstallProgress(this PooledBufferWriter writer, ToolInstallProgressDto progress)
    {
        writer.WriteString(progress.ToolId);
        writer.WriteInt32((int)progress.State);
        writer.WriteString(progress.Message ?? string.Empty);

        writer.WriteByte((byte)(progress.ProgressPercent.HasValue ? 1 : 0));
        if (progress.ProgressPercent.HasValue)
            writer.WriteInt32((int)(progress.ProgressPercent.Value * 100));

        writer.WriteString(progress.InstalledPath ?? string.Empty);
        writer.WriteString(progress.Error ?? string.Empty);
    }

    public static void WriteToolCatalogItem(this PooledBufferWriter writer, ToolCatalogItemDto tool)
    {
        writer.WriteString(tool.Id);
        writer.WriteString(tool.DisplayName);
        writer.WriteString(tool.Category);
        writer.WriteString(tool.Kind);
        writer.WriteString(tool.Description);
        writer.WriteByte((byte)(tool.CommandTemplate is not null ? 1 : 0));
        if (tool.CommandTemplate is not null)
        {
            writer.WriteString(tool.CommandTemplate);
        }
        writer.WriteString(tool.ExternalUrl ?? string.Empty);
        writer.WriteByte((byte)(tool.IsInstalled ? 1 : 0));
        writer.WriteByte((byte)(tool.IsInstallable ? 1 : 0));
        writer.WriteString(tool.InstalledPath ?? string.Empty);
        writer.WriteString(tool.Version ?? string.Empty);
        writer.WriteString(tool.Reason ?? string.Empty);
    }
}
