using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.CustomScript;

public static class CustomScriptProtocolSerializer
{
    public static byte[] SerializeCreateResult(Guid messageId, bool success, Guid scriptId)
        => BinaryProtocolSerializer.SerializeResultWithId(MessageType.CustomScriptCreateResult, messageId, success, scriptId);

    public static byte[] SerializeUpdateResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.CustomScriptUpdateResult, messageId, success);

    public static byte[] SerializeDeleteResult(Guid messageId, bool success)
        => BinaryProtocolSerializer.SerializeSimpleResult(MessageType.CustomScriptDeleteResult, messageId, success);

    public static byte[] SerializeListResult(Guid messageId, IEnumerable<CustomScriptDto> scripts)
        => BinaryProtocolSerializer.SerializeList(MessageType.CustomScriptListResult, messageId, scripts, (w, s) => w.WriteCustomScript(s));

    public static byte[] SerializeError(Guid messageId, string error)
        => BinaryProtocolSerializer.SerializeError(MessageType.CustomScriptOperationError, messageId, error);
}
