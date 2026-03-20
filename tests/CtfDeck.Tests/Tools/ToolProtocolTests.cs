using System.Text;
using CtfDeck.Contracts.Models.Tools;
using CtfDeck.Contracts.Protocols.Tools;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.Tools;

public class ToolProtocolTests
{
    [Fact]
    public void ToolInventoryRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInventoryRequest);
        writer.WriteGuid(messageId);
        var data = writer.ToArray();

        var request = new ToolInventoryRequest(data);

        request.MessageId.Should().Be(messageId);
    }

    [Fact]
    public void ToolInstallRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var toolIds = new List<string> { "nmap", "sqlmap" };

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.ToolInstallRequest);
        writer.WriteGuid(messageId);
        writer.WriteInt32(toolIds.Count);
        foreach (var id in toolIds)
        {
            writer.WriteString(id);
        }
        var data = writer.ToArray();

        var request = new ToolInstallRequest(data);

        request.MessageId.Should().Be(messageId);
        request.ToolIds.Should().HaveCount(2);
        request.ToolIds[0].Should().Be("nmap");
        request.ToolIds[1].Should().Be("sqlmap");
    }

    [Fact]
    public void IsToolMessage_ShouldDetectCorrectRange()
    {
        ToolProtocolDeserializer.IsToolMessage(MessageType.ToolInventoryRequest).Should().BeTrue();
        ToolProtocolDeserializer.IsToolMessage(MessageType.ToolInstallProgress).Should().BeTrue();
        ToolProtocolDeserializer.IsToolMessage(MessageType.SessionCreate).Should().BeFalse();
    }

    [Fact]
    public void SerializeInventoryResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var tools = new List<ToolStatusDto>
        {
            new() { Id = "nmap", DisplayName = "Nmap", Description = "Scanner", Kind = "Cli", IsInstalled = true, IsInstallable = false, Version = "7.92" },
            new() { Id = "ffuf", DisplayName = "ffuf", Description = "Fuzzer", Kind = "Cli", IsInstalled = false, IsInstallable = true }
        };

        var bytes = ToolProtocolSerializer.SerializeInventoryResult(messageId, tools);

        bytes[0].Should().Be((byte)MessageType.ToolInventoryResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        BitConverter.ToInt32(bytes.AsSpan(17, 4)).Should().Be(2);
    }

    [Fact]
    public void SerializeInstallProgress_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var progress = new ToolInstallProgressDto
        {
            ToolId = "nmap",
            State = ToolInstallState.Installing,
            Message = "Downloading...",
            ProgressPercent = 0.5f
        };

        var bytes = ToolProtocolSerializer.SerializeInstallProgress(messageId, progress);

        bytes[0].Should().Be((byte)MessageType.ToolInstallProgress);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
    }

    [Fact]
    public void SerializeCatalogSnapshot_ShouldRoundTrip()
    {
        var tools = new List<ToolCatalogItemDto>
        {
            new() { Id = "gobuster", DisplayName = "Gobuster", Category = "Web", Kind = "Cli", Description = "Dir buster", IsInstalled = true, IsInstallable = false }
        };

        var bytes = ToolProtocolSerializer.SerializeCatalogSnapshot(tools);

        bytes[0].Should().Be((byte)MessageType.ToolCatalogSnapshot);
        BitConverter.ToInt32(bytes.AsSpan(1, 4)).Should().Be(1);
    }
}
