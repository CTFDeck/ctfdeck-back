using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Protocols.WriteUp;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.WriteUp;

public class WriteUpProtocolTests
{
    [Fact]
    public void WriteUpCreateRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var name = "My CTF WriteUp";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpCreate);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        writer.WriteString(name);
        var data = writer.ToArray();

        // Act
        var request = new WriteUpCreateRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
        request.Name.Should().Be(name);
    }

    [Fact]
    public void WriteUpUpdateRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();
        var name = "Updated Title";
        var content = "# WriteUp\n\nThis is the **markdown** content.\n\n## Steps\n1. Recon\n2. Exploit";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpUpdate);
        writer.WriteGuid(messageId);
        writer.WriteGuid(writeUpId);
        writer.WriteString(name);
        writer.WriteString(content);
        var data = writer.ToArray();

        // Act
        var request = new WriteUpUpdateRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.WriteUpId.Should().Be(writeUpId);
        request.Name.Should().Be(name);
        request.Content.Should().Be(content);
    }

    [Fact]
    public void WriteUpDeleteRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpDelete);
        writer.WriteGuid(messageId);
        writer.WriteGuid(writeUpId);
        var data = writer.ToArray();

        // Act
        var request = new WriteUpDeleteRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.WriteUpId.Should().Be(writeUpId);
    }

    [Fact]
    public void WriteUpListRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpList);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        var data = writer.ToArray();

        // Act
        var request = new WriteUpListRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void WriteUpLoadRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpLoad);
        writer.WriteGuid(messageId);
        writer.WriteGuid(writeUpId);
        var data = writer.ToArray();

        // Act
        var request = new WriteUpLoadRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.WriteUpId.Should().Be(writeUpId);
    }

    [Fact]
    public void SerializeCreateResult_ShouldRoundTrip()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();

        // Act
        var bytes = WriteUpProtocolSerializer.SerializeCreateResult(messageId, true, writeUpId);

        // Assert
        bytes[0].Should().Be((byte)MessageType.WriteUpCreateResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1); // success
        var resultWriteUpId = new Guid(bytes.AsSpan(18, 16));
        resultWriteUpId.Should().Be(writeUpId);
    }

    [Fact]
    public void SerializeListResult_ShouldSerializeMultipleEntries()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var writeUps = new List<WriteUpMetadataDto>
        {
            new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Name = "WriteUp 1", CreatedAt = now, UpdatedAt = now },
            new() { Id = Guid.NewGuid(), SessionId = Guid.NewGuid(), Name = "WriteUp 2", CreatedAt = now, UpdatedAt = now }
        };

        // Act
        var bytes = WriteUpProtocolSerializer.SerializeListResult(messageId, writeUps);

        // Assert
        bytes[0].Should().Be((byte)MessageType.WriteUpListResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        var count = BitConverter.ToInt32(bytes.AsSpan(17, 4));
        count.Should().Be(2);
    }

    [Fact]
    public void SerializeLoadResult_Success_ShouldContainFullData()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var writeUp = new WriteUpDto
        {
            Id = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            Name = "Test WriteUp",
            Content = "# Hello\n\nWorld",
            CreatedAt = now,
            UpdatedAt = now
        };

        // Act
        var bytes = WriteUpProtocolSerializer.SerializeLoadResult(messageId, true, writeUp);

        // Assert
        bytes[0].Should().Be((byte)MessageType.WriteUpLoadResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1); // success
        var resultId = new Guid(bytes.AsSpan(18, 16));
        resultId.Should().Be(writeUp.Id);
    }

    [Fact]
    public void SerializeLoadResult_NotFound_ShouldBeMinimal()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var bytes = WriteUpProtocolSerializer.SerializeLoadResult(messageId, false, null);

        // Assert
        bytes[0].Should().Be((byte)MessageType.WriteUpLoadResult);
        bytes[17].Should().Be(0); // not success
        bytes.Length.Should().Be(18); // type + msgId + success only
    }

    [Fact]
    public void WriteUpUpdateRequest_WithUnicode_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var writeUpId = Guid.NewGuid();
        var name = "CTF Challenge: Buffer Overflow";
        var content = "## Exploit\n\nUsed pwntools to send payload:\n```python\npayload = b'A' * 64 + p64(0xdeadbeef)\n```";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpUpdate);
        writer.WriteGuid(messageId);
        writer.WriteGuid(writeUpId);
        writer.WriteString(name);
        writer.WriteString(content);
        var data = writer.ToArray();

        // Act
        var request = new WriteUpUpdateRequest(data);

        // Assert
        request.Name.Should().Be(name);
        request.Content.Should().Be(content);
    }

    [Fact]
    public void IsWriteUpMessage_ShouldDetectCorrectRange()
    {
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.WriteUpCreate).Should().BeTrue();
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.WriteUpUpdate).Should().BeTrue();
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.WriteUpDelete).Should().BeTrue();
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.WriteUpList).Should().BeTrue();
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.WriteUpLoad).Should().BeTrue();

        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.CustomScriptList).Should().BeFalse();
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.MediaUpload).Should().BeFalse();
        WriteUpProtocolDeserializer.IsWriteUpMessage(MessageType.SessionCreate).Should().BeFalse();
    }
}
