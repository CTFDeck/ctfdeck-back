using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.CustomScript;

public static class CustomScriptExtensions
{
    public static void WriteCustomScript(this PooledBufferWriter writer, CustomScriptDto script)
    {
        writer.WriteGuid(script.Id);
        writer.WriteString(script.Name);
        writer.WriteInt32((int)script.Category);
        writer.WriteString(script.Template);
    }
}
