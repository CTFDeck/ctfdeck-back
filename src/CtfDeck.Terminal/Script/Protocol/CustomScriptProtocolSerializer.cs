using CtfDeck.Terminal.Script.Models;
using CtfDeck.Terminal.WebSocket;

namespace CtfDeck.Terminal.Script.Protocol;

public static class CustomScriptProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid scriptId)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptCreateResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        writer.WriteGuid(scriptId);
        return writer.ToArray();
    }

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptUpdateResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        return writer.ToArray();
    }

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptDeleteResult);
        writer.WriteGuid(messageId);
        writer.WriteByte((byte)(success ? 1 : 0));
        return writer.ToArray();
    }

    public static byte[] SerializeListResult(Guid messageId, List<CustomScript> scripts)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptListResult);
        writer.WriteGuid(messageId);
        writer.WriteInt32(scripts.Count);

        foreach (var script in scripts)
        {
            WriteCustomScript(writer, script);
        }

        return writer.ToArray();
    }

    public static byte[] SerializeError(Guid messageId, string error)
    {
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptOperationError);
        writer.WriteGuid(messageId);
        writer.WriteString(error);
        return writer.ToArray();
    }

    private static void WriteCustomScript(PooledBufferWriter writer, CustomScript script)
    {
        writer.WriteGuid(script.Id);
        writer.WriteString(script.Name);
        writer.WriteInt32((int)script.Category);
        writer.WriteString(script.Template);
    }
}
