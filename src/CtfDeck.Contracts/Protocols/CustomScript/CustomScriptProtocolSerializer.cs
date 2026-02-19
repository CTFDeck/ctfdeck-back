using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.CustomScript;

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
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.CustomScriptUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.CustomScriptDeleteResult, messageId, success);

    public static byte[] SerializeListResult(Guid messageId, List<CustomScriptDto> scripts)
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
        => BinaryProtocolSerializer.SerializeError(MessageType.CustomScriptOperationError, messageId, error);

    private static void WriteCustomScript(PooledBufferWriter writer, CustomScriptDto script)
    {
        writer.WriteGuid(script.Id);
        writer.WriteString(script.Name);
        writer.WriteInt32((int)script.Category);
        writer.WriteString(script.Template);
    }
}
