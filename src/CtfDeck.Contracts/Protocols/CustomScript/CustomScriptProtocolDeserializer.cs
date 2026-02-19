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
        // Format: [1B type][16B msgId][4B nameLen][name][4B category][4B templateLen][template]
        MessageId = new Guid(data.Slice(1, 16));

        var offset = 17;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        Category = (ScriptCategory)BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        var templateLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Template = Encoding.UTF8.GetString(data.Slice(offset, templateLen));
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
        // Format: [1B type][16B msgId][16B scriptId][4B nameLen][name][4B category][4B templateLen][template]
        MessageId = new Guid(data.Slice(1, 16));
        ScriptId = new Guid(data.Slice(17, 16));

        var offset = 33;
        var nameLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Name = Encoding.UTF8.GetString(data.Slice(offset, nameLen));
        offset += nameLen;

        Category = (ScriptCategory)BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;

        var templateLen = BitConverter.ToInt32(data.Slice(offset, 4));
        offset += 4;
        Template = Encoding.UTF8.GetString(data.Slice(offset, templateLen));
    }
}

public readonly ref struct CustomScriptDeleteRequest
{
    public readonly Guid MessageId;
    public readonly Guid ScriptId;

    public CustomScriptDeleteRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId][16B scriptId]
        MessageId = new Guid(data.Slice(1, 16));
        ScriptId = new Guid(data.Slice(17, 16));
    }
}

public readonly ref struct CustomScriptListRequest
{
    public readonly Guid MessageId;

    public CustomScriptListRequest(ReadOnlySpan<byte> data)
    {
        // Format: [1B type][16B msgId]
        MessageId = new Guid(data.Slice(1, 16));
    }
}

public static class CustomScriptProtocolDeserializer
{
    public static bool IsCustomScriptMessage(MessageType type)
    {
        return type >= MessageType.CustomScriptCreate && type <= MessageType.CustomScriptList;
    }
}
