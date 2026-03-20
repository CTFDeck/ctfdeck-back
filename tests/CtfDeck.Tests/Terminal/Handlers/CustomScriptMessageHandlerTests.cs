using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Protocols.CustomScript;
using CtfDeck.Contracts.Transport;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Scripts;
using CtfDeck.Terminal.Features.Scripts;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Handlers;

public class CustomScriptMessageHandlerTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly CustomScriptService _service;
    private readonly CustomScriptMessageHandler _handler;

    public CustomScriptMessageHandlerTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        var scriptRepo = new CustomScriptRepository(_db);
        _service = new CustomScriptService(scriptRepo);
        _handler = new CustomScriptMessageHandler(_service);
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
        writer.WriteByte((byte)MessageType.CustomScriptCreate);
        writer.WriteGuid(msgId);
        writer.WriteString("My Script");
        writer.WriteInt32((int)ScriptCategory.Discovery);
        writer.WriteString("nmap {{target}}");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.CustomScriptCreateResult);
        response[17].Should().Be(1); // Success
        var scriptId = new Guid(response.AsSpan(18, 16));
        
        var scripts = _service.GetAll();
        scripts.Should().ContainSingle(s => s.Id == scriptId && s.Name == "My Script");
    }

    [Fact]
    public async Task Update_ShouldReturnSuccess()
    {
        var script = _service.Create("Old Name", ScriptCategory.Other, "echo old");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptUpdate);
        writer.WriteGuid(msgId);
        writer.WriteGuid(script.Id);
        writer.WriteString("New Name");
        writer.WriteInt32((int)ScriptCategory.Exploit);
        writer.WriteString("echo new");
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.CustomScriptUpdateResult);
        response[17].Should().Be(1); // Success
        
        var updated = _service.GetAll().First(s => s.Id == script.Id);
        updated.Name.Should().Be("New Name");
        updated.Template.Should().Be("echo new");
    }

    [Fact]
    public async Task Delete_ShouldReturnSuccess()
    {
        var script = _service.Create("To Delete", ScriptCategory.Other, "echo del");
        var msgId = Guid.NewGuid();
        
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptDelete);
        writer.WriteGuid(msgId);
        writer.WriteGuid(script.Id);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.CustomScriptDeleteResult);
        response[17].Should().Be(1); // Success
        
        _service.GetAll().Should().NotContain(s => s.Id == script.Id);
    }

    [Fact]
    public async Task List_ShouldReturnScripts()
    {
        _service.Create("S1", ScriptCategory.Other, "c1");
        _service.Create("S2", ScriptCategory.Other, "c2");
        
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.CustomScriptList);
        writer.WriteGuid(msgId);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.CustomScriptListResult);
        var count = BitConverter.ToInt32(response.AsSpan(17, 4));
        count.Should().Be(2);
    }
}
