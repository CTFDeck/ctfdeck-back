using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal;

public class BinaryProtocolTests
{
    [Fact]
    public void WebSocketCommandReader_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var cmd = "ls -la";
        var cmdBytes = System.Text.Encoding.UTF8.GetBytes(cmd);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CommandExecute);
        writer.WriteInt32(cmdBytes.Length);
        writer.WriteBytes(cmdBytes);
        writer.WriteGuid(messageId);
        var data = writer.ToArray();

        var reader = new WebSocketCommandReader(data);

        reader.CommandLength.Should().Be(cmdBytes.Length);
        reader.GetCommand().Should().Be(cmd);
        reader.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void CommandKillReader_ShouldDeserializeCorrectly()
    {
        var commandId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CommandKill);
        writer.WriteGuid(commandId);
        var data = writer.ToArray();

        var reader = new CommandKillReader(data);

        reader.CommandId.Should().Be(commandId);
    }

    [Fact]
    public void PasswordProvideReader_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var password = "secret_password";
        var passBytes = System.Text.Encoding.UTF8.GetBytes(password);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.PasswordProvide);
        writer.WriteGuid(messageId);
        writer.WriteInt32(passBytes.Length);
        writer.WriteBytes(passBytes);
        var data = writer.ToArray();

        var reader = new PasswordProvideReader(data);

        reader.MessageId.Should().Be(messageId);
        reader.PasswordLength.Should().Be(passBytes.Length);
        reader.GetPassword().Should().Be(password);
    }

    [Fact]
    public void PooledBufferWriter_ShouldResizeCorrectly()
    {
        using var writer = new PooledBufferWriter(10);
        var largeData = new byte[100];
        new Random().NextBytes(largeData);

        writer.WriteBytes(largeData);

        writer.Length.Should().Be(100);
        writer.ToArray().Should().BeEquivalentTo(largeData);
    }

    [Fact]
    public void BinaryProtocolSerializer_SerializeStreamEnd_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var exitCode = 0;
        var wd = "/home/ctf";

        var bytes = TerminalProtocolSerializer.SerializeStreamEnd(messageId, exitCode, wd);

        var decoded = StreamEndMessage.Deserialize(bytes);
        decoded.MessageId.Should().Be(messageId);
        decoded.ExitCode.Should().Be(exitCode);
        decoded.WorkingDirectory.Should().Be(wd);
    }

    [Fact]
    public void BinaryProtocolSerializer_SerializeCommandKillResult_ShouldRoundTrip()
    {
        var commandId = Guid.NewGuid();

        var bytes = TerminalProtocolSerializer.SerializeCommandKillResult(commandId, true);

        bytes[0].Should().Be((byte)MessageType.CommandKillResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(commandId);
        bytes[17].Should().Be(1);
    }

    [Fact]
    public void WebSocketResponse_SerializeDeserialize_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var response = WebSocketResponse.FromResult(0, "output", "error", "/wd", messageId);

        var bytes = response.Serialize();
        var deserialized = WebSocketResponse.Deserialize(bytes);

        deserialized.MessageId.Should().Be(messageId);
        deserialized.ExitCode.Should().Be(0);
        deserialized.Output.Should().Be("output");
        deserialized.Error.Should().Be("error");
        deserialized.WorkingDirectory.Should().Be("/wd");
    }
}
