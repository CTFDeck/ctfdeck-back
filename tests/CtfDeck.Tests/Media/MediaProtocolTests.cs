using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Protocols.Media;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.Media;

public class MediaProtocolTests
{
    [Fact]
    public void MediaUploadRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var fileName = "screenshot.png";
        var mimeType = "image/png";
        var fileData = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }; // PNG magic bytes

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaUpload);
        writer.WriteGuid(messageId);
        writer.WriteString(fileName);
        writer.WriteString(mimeType);
        writer.WriteInt32(fileData.Length);
        writer.WriteBytes(fileData);
        var data = writer.ToArray();

        // Act
        var request = new MediaUploadRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.FileName.Should().Be(fileName);
        request.MimeType.Should().Be(mimeType);
        request.Data.ToArray().Should().BeEquivalentTo(fileData);
    }

    [Fact]
    public void MediaLoadRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaLoad);
        writer.WriteGuid(messageId);
        writer.WriteGuid(mediaId);
        var data = writer.ToArray();

        // Act
        var request = new MediaLoadRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.MediaId.Should().Be(mediaId);
    }

    [Fact]
    public void MediaDeleteRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaDelete);
        writer.WriteGuid(messageId);
        writer.WriteGuid(mediaId);
        var data = writer.ToArray();

        // Act
        var request = new MediaDeleteRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
        request.MediaId.Should().Be(mediaId);
    }

    [Fact]
    public void MediaListRequest_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaList);
        writer.WriteGuid(messageId);
        var data = writer.ToArray();

        // Act
        var request = new MediaListRequest(data);

        // Assert
        request.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void SerializeUploadResult_ShouldRoundTrip()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();

        // Act
        var bytes = MediaProtocolSerializer.SerializeUploadResult(messageId, true, mediaId);

        // Assert
        bytes[0].Should().Be((byte)MessageType.MediaUploadResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1); // success
        var resultMediaId = new Guid(bytes.AsSpan(18, 16));
        resultMediaId.Should().Be(mediaId);
    }

    [Fact]
    public void SerializeLoadResult_Success_ShouldContainFullData()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var fileData = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 }; // JPEG magic
        var media = new MediaDto
        {
            Id = Guid.NewGuid(),
            FileName = "photo.jpg",
            MimeType = "image/jpeg",
            Data = fileData,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var bytes = MediaProtocolSerializer.SerializeLoadResult(messageId, true, media.Id, media.Data);

        // Assert
        bytes[0].Should().Be((byte)MessageType.MediaLoadResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1); // success
        var resultMediaId = new Guid(bytes.AsSpan(18, 16));
        resultMediaId.Should().Be(media.Id);
    }

    [Fact]
    public void SerializeLoadResult_NotFound_ShouldBeMinimal()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var bytes = MediaProtocolSerializer.SerializeLoadResult(messageId, false, Guid.Empty, null);

        // Assert
        bytes[0].Should().Be((byte)MessageType.MediaLoadResult);
        bytes[17].Should().Be(0); // not success
        bytes.Length.Should().Be(18); // type + msgId + success only
    }

    [Fact]
    public void SerializeListResult_ShouldSerializeMultipleEntries()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        var mediaList = new List<MediaMetadataDto>
        {
            new() { Id = Guid.NewGuid(), FileName = "screen1.png", MimeType = "image/png", CreatedAt = now },
            new() { Id = Guid.NewGuid(), FileName = "exploit.py", MimeType = "text/x-python", CreatedAt = now },
            new() { Id = Guid.NewGuid(), FileName = "flag.txt", MimeType = "text/plain", CreatedAt = now }
        };

        // Act
        var bytes = MediaProtocolSerializer.SerializeListResult(messageId, mediaList);

        // Assert
        bytes[0].Should().Be((byte)MessageType.MediaListResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        var count = BitConverter.ToInt32(bytes.AsSpan(17, 4));
        count.Should().Be(3);
    }

    [Fact]
    public void SerializeDeleteResult_ShouldWork()
    {
        // Arrange
        var messageId = Guid.NewGuid();

        // Act
        var bytes = MediaProtocolSerializer.SerializeDeleteResult(messageId, true);

        // Assert
        bytes[0].Should().Be((byte)MessageType.MediaDeleteResult);
        var resultMsgId = new Guid(bytes.AsSpan(1, 16));
        resultMsgId.Should().Be(messageId);
        bytes[17].Should().Be(1);
    }

    [Fact]
    public void MediaUploadRequest_LargeData_ShouldDeserializeCorrectly()
    {
        // Arrange
        var messageId = Guid.NewGuid();
        var fileName = "large_image.bmp";
        var mimeType = "image/bmp";
        var fileData = new byte[8192];
        new Random(42).NextBytes(fileData);

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaUpload);
        writer.WriteGuid(messageId);
        writer.WriteString(fileName);
        writer.WriteString(mimeType);
        writer.WriteInt32(fileData.Length);
        writer.WriteBytes(fileData);
        var data = writer.ToArray();

        // Act
        var request = new MediaUploadRequest(data);

        // Assert
        request.FileName.Should().Be(fileName);
        request.MimeType.Should().Be(mimeType);
        request.Data.Length.Should().Be(8192);
        request.Data.ToArray().Should().BeEquivalentTo(fileData);
    }

    [Fact]
    public void IsMediaMessage_ShouldDetectCorrectRange()
    {
        MediaProtocolDeserializer.IsMediaMessage(MessageType.MediaUpload).Should().BeTrue();
        MediaProtocolDeserializer.IsMediaMessage(MessageType.MediaLoad).Should().BeTrue();
        MediaProtocolDeserializer.IsMediaMessage(MessageType.MediaDelete).Should().BeTrue();
        MediaProtocolDeserializer.IsMediaMessage(MessageType.MediaList).Should().BeTrue();

        MediaProtocolDeserializer.IsMediaMessage(MessageType.WriteUpCreate).Should().BeFalse();
        MediaProtocolDeserializer.IsMediaMessage(MessageType.CustomScriptList).Should().BeFalse();
        MediaProtocolDeserializer.IsMediaMessage(MessageType.SessionCreate).Should().BeFalse();
    }
}
