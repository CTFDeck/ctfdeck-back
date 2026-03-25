using System.Text;
using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Protocols.CustomScript;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.CustomScript;

public class CustomScriptProtocolTests
{
    [Fact]
    public void CustomScriptCreateRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var name = "Nmap Discovery";
        var category = ScriptCategory.Discovery;
        var template = "nmap -sV {target}";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptCreate);
        writer.WriteGuid(messageId);
        writer.WriteString(name);
        writer.WriteInt32((int)category);
        writer.WriteString(template);
        var data = writer.ToArray();

        var request = new CustomScriptCreateRequest(data);

        request.MessageId.Should().Be(messageId);
        request.Name.Should().Be(name);
        request.Category.Should().Be(category);
        request.Template.Should().Be(template);
    }

    [Fact]
    public void CustomScriptUpdateRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();
        var name = "Updated Script";
        var category = ScriptCategory.Exploit;
        var template = "python3 {script} {target}";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptUpdate);
        writer.WriteGuid(messageId);
        writer.WriteGuid(scriptId);
        writer.WriteString(name);
        writer.WriteInt32((int)category);
        writer.WriteString(template);
        var data = writer.ToArray();

        var request = new CustomScriptUpdateRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ScriptId.Should().Be(scriptId);
        request.Name.Should().Be(name);
        request.Category.Should().Be(category);
        request.Template.Should().Be(template);
    }

    [Fact]
    public void CustomScriptDeleteRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptDelete);
        writer.WriteGuid(messageId);
        writer.WriteGuid(scriptId);
        var data = writer.ToArray();

        var request = new CustomScriptDeleteRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ScriptId.Should().Be(scriptId);
    }

    [Fact]
    public void CustomScriptListRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptList);
        writer.WriteGuid(messageId);
        var data = writer.ToArray();

        var request = new CustomScriptListRequest(data);

        request.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void IsCustomScriptMessage_ShouldDetectCorrectRange()
    {
        CustomScriptProtocolDeserializer.IsCustomScriptMessage(MessageType.CustomScriptCreate).Should().BeTrue();
        CustomScriptProtocolDeserializer.IsCustomScriptMessage(MessageType.CustomScriptList).Should().BeTrue();
        CustomScriptProtocolDeserializer.IsCustomScriptMessage(MessageType.SessionCreate).Should().BeFalse();
    }

    [Fact]
    public void SerializeCreateResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var scriptId = Guid.NewGuid();

        var bytes = CustomScriptProtocolSerializer.SerializeCreateResult(messageId, true, scriptId);

        bytes[0].Should().Be((byte)MessageType.CustomScriptCreateResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        bytes[17].Should().Be(1);
        new Guid(bytes.AsSpan(18, 16)).Should().Be(scriptId);
    }

    [Fact]
    public void SerializeListResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var scripts = new List<CustomScriptDto>
        {
            new() { Id = Guid.NewGuid(), Name = "Script 1", Category = ScriptCategory.Web, Template = "ls" },
            new() { Id = Guid.NewGuid(), Name = "Script 2", Category = ScriptCategory.Other, Template = "whoami" }
        };

        var bytes = CustomScriptProtocolSerializer.SerializeListResult(messageId, scripts);

        bytes[0].Should().Be((byte)MessageType.CustomScriptListResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        BitConverter.ToInt32(bytes.AsSpan(17, 4)).Should().Be(2);
    }
}
