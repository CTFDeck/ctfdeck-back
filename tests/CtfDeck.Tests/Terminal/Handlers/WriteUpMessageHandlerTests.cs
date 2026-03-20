using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Contracts.Protocols.WriteUp;
using CtfDeck.Contracts.Transport;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.WriteUps;
using CtfDeck.Terminal.Features.WriteUps;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Handlers;

public class WriteUpMessageHandlerTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly WriteUpService _service;
    private readonly WriteUpMessageHandler _handler;

    public WriteUpMessageHandlerTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        var writeUpRepo = new WriteUpRepository(_db);
        _service = new WriteUpService(writeUpRepo);
        _handler = new WriteUpMessageHandler(_service);
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
    public async Task Create_ShouldReturnSuccess()
    {
        var msgId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpCreate);
        writer.WriteGuid(msgId);
        writer.WriteGuid(sessionId);
        writer.WriteString("My WriteUp");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.WriteUpCreateResult);
        response[17].Should().Be(1); // Success
        var writeUpId = new Guid(response.AsSpan(18, 16));
        
        var writeUp = _service.GetById(writeUpId);
        writeUp.Should().NotBeNull();
        writeUp!.Name.Should().Be("My WriteUp");
    }

    [Fact]
    public async Task Load_ShouldReturnWriteUp()
    {
        var writeUp = _service.Create(Guid.NewGuid(), "My WriteUp");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpLoad);
        writer.WriteGuid(msgId);
        writer.WriteGuid(writeUp.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.WriteUpLoadResult);
        response[17].Should().Be(1); // Success
    }

    [Fact]
    public async Task List_ShouldReturnWriteUps()
    {
        var sessionId = Guid.NewGuid();
        _service.Create(sessionId, "WU1");
        _service.Create(sessionId, "WU2");
        
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpList);
        writer.WriteGuid(msgId);
        writer.WriteGuid(sessionId);
        writer.WriteInt32(0); // Offset
        writer.WriteInt32(10); // Limit
        writer.WriteByte(0); // UnassignedOnly
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.WriteUpListResult);
        var totalCount = BitConverter.ToInt32(response.AsSpan(17, 4));
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task Update_ShouldReturnSuccess()
    {
        var writeUp = _service.Create(Guid.NewGuid(), "Old Name");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpUpdate);
        writer.WriteGuid(msgId);
        writer.WriteGuid(writeUp.Id);
        writer.WriteString("New Name");
        writer.WriteString("New Content");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.WriteUpUpdateResult);
        response[17].Should().Be(1); // Success
        
        var updated = _service.GetById(writeUp.Id);
        updated!.Name.Should().Be("New Name");
        updated.Content.Should().Be("New Content");
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess()
    {
        var writeUp = _service.Create(Guid.NewGuid(), "To Delete");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.WriteUpDelete);
        writer.WriteGuid(msgId);
        writer.WriteGuid(writeUp.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.WriteUpDeleteResult);
        response[17].Should().Be(1); // Success
        
        _service.GetById(writeUp.Id).Should().BeNull();
    }
}
