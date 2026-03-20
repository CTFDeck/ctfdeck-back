using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;
using CtfDeck.Contracts.Protocols.Tools;
using CtfDeck.Contracts.Transport;
using CtfDeck.Terminal.Handlers;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Handlers;

public class ToolMessageHandlerTests
{
    private class FakeToolInstallationCoordinator : IToolInstallationCoordinator
    {
        public Task<IReadOnlyCollection<ToolStatusDto>> GetInventoryAsync(CancellationToken cancellationToken = default)
        {
            var inventory = new List<ToolStatusDto>();
            return Task.FromResult<IReadOnlyCollection<ToolStatusDto>>(inventory);
        }

        public async Task InstallAsync(IReadOnlyCollection<string> toolIds, Func<ToolInstallProgressDto, Task> onProgress, CancellationToken cancellationToken = default)
        {
            foreach (var tool in toolIds)
            {
                await onProgress(new ToolInstallProgressDto
                {
                    ToolId = tool,
                    State = ToolInstallState.Downloading,
                    ProgressPercent = 50,
                    Message = "Halfway there"
                });
            }
        }
    }

    private readonly FakeToolInstallationCoordinator _fakeCoordinator;
    private readonly ToolMessageHandler _handler;

    public ToolMessageHandlerTests()
    {
        _fakeCoordinator = new FakeToolInstallationCoordinator();
        _handler = new ToolMessageHandler(_fakeCoordinator);
    }

    private async Task<byte[]> SendMessageAsync(byte[] requestData, List<byte[]>? progressResponses = null)
    {
        byte[]? finalResponse = null;
        await _handler.TryHandleAsync("client1", requestData, data =>
        {
            if (progressResponses != null && 
               (data[0] == (byte)MessageType.ToolInstallProgress || data[0] == (byte)MessageType.ToolInventoryResult))
            {
                progressResponses.Add(data);
            }
            else
            {
                finalResponse = data;
            }
            return Task.CompletedTask;
        }, CancellationToken.None);
        
        return finalResponse!;
    }

    [Fact]
    public async Task InventoryRequest_ShouldReturnInventory()
    {
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInventoryRequest);
        writer.WriteGuid(msgId);
        
        var response = await SendMessageAsync(writer.ToArray());

        response[0].Should().Be((byte)MessageType.ToolInventoryResult);
        var resMsgId = new Guid(response.AsSpan(1, 16));
        resMsgId.Should().Be(msgId);
    }

    [Fact]
    public async Task InstallRequest_ShouldReturnAcceptedAndProgress()
    {
        var msgId = Guid.NewGuid();
        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInstallRequest);
        writer.WriteGuid(msgId);
        writer.WriteInt32(1);
        writer.WriteString("my-tool");
        
        var progressResponses = new List<byte[]>();
        var response = await SendMessageAsync(writer.ToArray(), progressResponses);

        response[0].Should().Be((byte)MessageType.ToolInstallAccepted);
        var resMsgId = new Guid(response.AsSpan(1, 16));
        resMsgId.Should().Be(msgId);

        // Allow background task to execute
        await Task.Delay(100);
        
        progressResponses.Should().Contain(x => x[0] == (byte)MessageType.ToolInstallProgress);
        progressResponses.Should().Contain(x => x[0] == (byte)MessageType.ToolInventoryResult);
    }
}
