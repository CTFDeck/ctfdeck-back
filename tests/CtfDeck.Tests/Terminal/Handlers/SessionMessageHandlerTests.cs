using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Protocols.Session;
using CtfDeck.Contracts.Transport;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Terminal.Features.Sessions;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Handlers;

public class SessionMessageHandlerTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly SessionService _service;
    private readonly ActiveSessionManager _activeSessionManager;
    private readonly SessionMessageHandler _handler;

    public SessionMessageHandlerTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        var sessionRepo = new SessionRepository(_db);
        _service = new SessionService(sessionRepo);
        _activeSessionManager = new ActiveSessionManager(_service);
        _handler = new SessionMessageHandler(_service, _activeSessionManager);
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
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionCreate);
        writer.WriteGuid(msgId);
        writer.WriteString("Test Session");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.SessionCreateResult);
        response[17].Should().Be(1); // Success
        var sessionId = new Guid(response.AsSpan(18, 16));
        
        var session = _service.GetById(sessionId);
        session.Should().NotBeNull();
        session!.Name.Should().Be("Test Session");
    }

    [Fact]
    public async Task SetActive_ShouldReturnSuccess()
    {
        var session = _service.Create("My Session");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionSetActive);
        writer.WriteGuid(msgId);
        writer.WriteGuid(session.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.SessionSetActiveResult);
        response[17].Should().Be(1); // Success
        
        _activeSessionManager.GetActiveSession("client1").Should().Be(session.Id);
    }

    [Fact]
    public async Task Load_ShouldReturnSession()
    {
        var session = _service.Create("My Session");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionLoad);
        writer.WriteGuid(msgId);
        writer.WriteGuid(session.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.SessionLoadResult);
        response[17].Should().Be(1); // Success
    }

    [Fact]
    public async Task List_ShouldReturnSessions()
    {
        _service.Create("Session1");
        _service.Create("Session2");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionList);
        writer.WriteGuid(msgId);
        writer.WriteInt32(0); // Offset
        writer.WriteInt32(10); // Limit
        writer.WriteByte(0); // UnassignedOnly
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.SessionListResult);
        var totalCount = BitConverter.ToInt32(response.AsSpan(17, 4));
        totalCount.Should().Be(2);
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess()
    {
        var session = _service.Create("To Delete");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionDelete);
        writer.WriteGuid(msgId);
        writer.WriteGuid(session.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.SessionDeleteResult);
        response[17].Should().Be(1); // Success
        
        _service.GetById(session.Id).Should().BeNull();
    }

    [Fact]
    public async Task Update_ShouldReturnSuccess()
    {
        var session = _service.Create("Old Name");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionUpdate);
        writer.WriteGuid(msgId);
        writer.WriteGuid(session.Id);
        writer.WriteString("New Name");
        writer.WriteString("New Desc");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.SessionUpdateResult);
        response[17].Should().Be(1); // Success
        
        var updated = _service.GetById(session.Id);
        updated!.Name.Should().Be("New Name");
        updated.Description.Should().Be("New Desc");
    }

    [Fact]
    public async Task AddEditDeleteTarget_ShouldWork()
    {
        var session = _service.Create("Target Session");
        
        // Add
        var addMsgId = Guid.NewGuid();
        using var addWriter = new PooledBufferWriter();
        addWriter.WriteByte((byte)MessageType.SessionAddTarget);
        addWriter.WriteGuid(addMsgId);
        addWriter.WriteGuid(session.Id);
        
        var targetId = Guid.NewGuid();
        addWriter.WriteGuid(targetId);
        addWriter.WriteString("10.10.10.10");
        addWriter.WriteInt32(80);
        addWriter.WriteString("Web");
        addWriter.WriteString("My Web Server");
        addWriter.WriteInt32((int)TargetType.Web);
        
        var addResp = await SendMessageAsync(addWriter.ToArray());
        addResp[0].Should().Be((byte)MessageType.SessionAddTargetResult);
        addResp[17].Should().Be(1);
        
        // Edit
        var editMsgId = Guid.NewGuid();
        using var editWriter = new PooledBufferWriter();
        editWriter.WriteByte((byte)MessageType.SessionEditTarget);
        editWriter.WriteGuid(editMsgId);
        editWriter.WriteGuid(session.Id);
        
        editWriter.WriteGuid(targetId);
        editWriter.WriteString("10.10.10.11"); // changed IP
        editWriter.WriteInt32(443);
        editWriter.WriteString("Web Secure");
        editWriter.WriteString("My Web Server Secure");
        editWriter.WriteInt32((int)TargetType.Web);
        
        var editResp = await SendMessageAsync(editWriter.ToArray());
        editResp[0].Should().Be((byte)MessageType.SessionEditTargetResult);
        editResp[17].Should().Be(1);
        
        // Delete
        var delMsgId = Guid.NewGuid();
        using var delWriter = new PooledBufferWriter();
        delWriter.WriteByte((byte)MessageType.SessionDeleteTarget);
        delWriter.WriteGuid(delMsgId);
        delWriter.WriteGuid(session.Id);
        delWriter.WriteGuid(targetId);
        
        var delResp = await SendMessageAsync(delWriter.ToArray());
        delResp[0].Should().Be((byte)MessageType.SessionDeleteTargetResult);
        delResp[17].Should().Be(1);
    }
}
