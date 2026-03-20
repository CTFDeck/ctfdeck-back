using System.Text;
using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Transport;

namespace CtfDeck.Contracts.Protocols.CustomScript;

public readonly ref struct CustomScriptCreateRequest
{
    public readonly Guid MessageId;
    public readonly string Name;
    public readonly ScriptCategory Category;
    public readonly string Template;

    public CustomScriptCreateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        Name = reader.ReadString();
        Category = (ScriptCategory)reader.ReadInt32();
        Template = reader.ReadString();
    }
}

public readonly ref struct CustomScriptUpdateRequest
{
    public readonly Guid MessageId;
    public readonly Guid ScriptId;
    public readonly string Name;
    public readonly ScriptCategory Category;
    public readonly string Template;

    public CustomScriptUpdateRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ScriptId = reader.ReadGuid();
        Name = reader.ReadString();
        Category = (ScriptCategory)reader.ReadInt32();
        Template = reader.ReadString();
    }
}

public readonly ref struct CustomScriptDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid ScriptId;

    public CustomScriptDeleteRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
        ScriptId = reader.ReadGuid();
    }
}

public readonly ref struct CustomScriptListRequest
{
    public readonly Guid MessageId;

    public CustomScriptListRequest(ReadOnlySpan<byte> data)
    {
        var reader = new BinaryProtocolReader(data);
        (_, MessageId) = reader.ReadHeader();
    }
}

public static class CustomScriptProtocolDeserializer
{
    public static bool IsCustomScriptMessage(MessageType type)
    {
        return type >= MessageType.CustomScriptCreate && type <= MessageType.CustomScriptList;
    }
}
