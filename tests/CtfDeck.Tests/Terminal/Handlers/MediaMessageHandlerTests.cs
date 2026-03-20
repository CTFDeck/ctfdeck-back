using CtfDeck.Contracts.Models.Media;
using CtfDeck.Contracts.Protocols.Media;
using CtfDeck.Contracts.Transport;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Media;
using CtfDeck.Terminal.Features.Media;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Handlers;

public class MediaMessageHandlerTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly MediaService _service;
    private readonly MediaMessageHandler _handler;

    public MediaMessageHandlerTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        var mediaRepo = new MediaRepository(_db);
        _service = new MediaService(mediaRepo);
        _handler = new MediaMessageHandler(_service);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    private async Task<byte[]> SendMessageAsync(byte[] requestData)
    {
        byte[]? responseData = null;
        await _handler.TryHandleAsync("client1", requestData, data =>
        {
            responseData = data;
            return Task.CompletedTask;
        }, CancellationToken.None);
        
        return responseData!;
    }

    [Fact]
    public async Task Upload_ShouldReturnSuccess()
    {
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaUpload);
        writer.WriteGuid(msgId);
        writer.WriteString("test.png");
        writer.WriteString("image/png");
        
        var bytes = new byte[] { 1, 2, 3, 4 };
        writer.WriteInt32(bytes.Length);
        writer.WriteBytes(bytes);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.MediaUploadResult);
        response[17].Should().Be(1); // Success
        var mediaId = new Guid(response.AsSpan(18, 16));
        
        var media = _service.GetById(mediaId);
        media.Should().NotBeNull();
        media!.FileName.Should().Be("test.png");
        media.Data.Should().BeEquivalentTo(bytes);
    }

    [Fact]
    public async Task Load_ShouldReturnMedia()
    {
        var media = _service.Create("doc.pdf", "application/pdf", new byte[] { 0x41 });
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaLoad);
        writer.WriteGuid(msgId);
        writer.WriteGuid(media.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.MediaLoadResult);
        response[17].Should().Be(1); // Success
    }

    [Fact]
    public async Task List_ShouldReturnMedia()
    {
        _service.Create("img1.png", "image/png", new byte[] { 1 });
        _service.Create("img2.png", "image/png", new byte[] { 2 });
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaList);
        writer.WriteGuid(msgId);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.MediaListResult);
        var count = BitConverter.ToInt32(response.AsSpan(17, 4));
        count.Should().Be(2);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess()
    {
        var media = _service.Create("To Delete", "text/plain", new byte[0]);
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.MediaDelete);
        writer.WriteGuid(msgId);
        writer.WriteGuid(media.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.MediaDeleteResult);
        response[17].Should().Be(1); // Success
        
        _service.GetById(media.Id).Should().BeNull();
    }
}
