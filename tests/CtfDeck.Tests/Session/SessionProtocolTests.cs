using System.Text;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Protocols.Session;
using CtfDeck.Contracts.Transport;
using FluentAssertions;

namespace CtfDeck.Tests.Session;

public class SessionProtocolTests
{
    [Fact]
    public void SessionCreateRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var name = "My Session";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionCreate);
        writer.WriteGuid(messageId);
        writer.WriteString(name);
        var data = writer.ToArray();

        var request = new SessionCreateRequest(data);

        request.MessageId.Should().Be(messageId);
        request.Name.Should().Be(name);
    }

    [Fact]
    public void SessionSetActiveRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionSetActive);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        var data = writer.ToArray();

        var request = new SessionSetActiveRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void SessionLoadRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionLoad);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        var data = writer.ToArray();

        var request = new SessionLoadRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void SessionListRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var offset = 10;
        var limit = 20;
        var unassignedOnly = true;

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionList);
        writer.WriteGuid(messageId);
        writer.WriteInt32(offset);
        writer.WriteInt32(limit);
        writer.WriteByte((byte)(unassignedOnly ? 1 : 0));
        var data = writer.ToArray();

        var request = new SessionListRequest(data);

        request.MessageId.Should().Be(messageId);
        request.Offset.Should().Be(offset);
        request.Limit.Should().Be(limit);
        request.UnassignedOnly.Should().BeTrue();
    }

    [Fact]
    public void SessionDeleteRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionDelete);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        var data = writer.ToArray();

        var request = new SessionDeleteRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
    }

    [Fact]
    public void SessionUpdateRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var name = "Updated Session";
        var description = "Updated Description";

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionUpdate);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        writer.WriteString(name);
        writer.WriteString(description);
        var data = writer.ToArray();

        var request = new SessionUpdateRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
        request.Name.Should().Be(name);
        request.Description.Should().Be(description);
    }

    [Fact]
    public void SessionAddTargetRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var target = new SessionTargetDto
        {
            Address = "192.168.1.10",
            Port = 80,
            Name = "Web Server",
            Description = "Apache",
            Type = TargetType.Web
        };

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionAddTarget);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        writer.WriteGuid(Guid.Empty); // Target.Id (lu par ReadTarget() sans argument)
        writer.WriteString(target.Address);
        writer.WriteInt32(target.Port ?? -1);
        writer.WriteString(target.Name);
        writer.WriteString(target.Description);
        writer.WriteInt32((int)target.Type);
        var data = writer.ToArray();

        var request = new SessionAddTargetRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
        request.Target.Address.Should().Be(target.Address);
        request.Target.Port.Should().Be(target.Port);
        request.Target.Name.Should().Be(target.Name);
        request.Target.Description.Should().Be(target.Description);
        request.Target.Type.Should().Be(target.Type);
    }

    [Fact]
    public void SessionDeleteTargetRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var targetId = Guid.NewGuid();

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionDeleteTarget);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        writer.WriteGuid(targetId);
        var data = writer.ToArray();

        var request = new SessionDeleteTargetRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
        request.TargetId.Should().Be(targetId);
    }

    [Fact]
    public void SessionEditTargetRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var targetId = Guid.NewGuid();
        var target = new SessionTargetDto
        {
            Id = targetId,
            Address = "10.0.0.5",
            Port = 443,
            Name = "HTTPS Target",
            Description = "SSL enabled",
            Type = TargetType.Web
        };

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionEditTarget);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        writer.WriteGuid(targetId);
        writer.WriteString(target.Address);
        writer.WriteInt32(target.Port ?? -1);
        writer.WriteString(target.Name);
        writer.WriteString(target.Description);
        writer.WriteInt32((int)target.Type);
        var data = writer.ToArray();

        var request = new SessionEditTargetRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
        request.Target.Id.Should().Be(targetId);
        request.Target.Address.Should().Be(target.Address);
        request.Target.Port.Should().Be(target.Port);
        request.Target.Name.Should().Be(target.Name);
        request.Target.Description.Should().Be(target.Description);
        request.Target.Type.Should().Be(target.Type);
    }

    [Fact]
    public void SessionUpdateTargetsRequest_ShouldDeserializeCorrectly()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var targets = new List<SessionTargetDto>
        {
            new() { Id = Guid.NewGuid(), Address = "1.1.1.1", Port = 53, Name = "DNS", Type = TargetType.Misc },
            new() { Id = Guid.NewGuid(), Address = "8.8.8.8", Port = 53, Name = "Google DNS", Type = TargetType.Misc }
        };

        using var writer = new PooledBufferWriter();
        writer.WriteByte((byte)MessageType.SessionUpdateTargets);
        writer.WriteGuid(messageId);
        writer.WriteGuid(sessionId);
        writer.WriteInt32(targets.Count);
        foreach (var t in targets)
        {
            writer.WriteGuid(t.Id);
            writer.WriteString(t.Address);
            writer.WriteInt32(t.Port ?? -1);
            writer.WriteString(t.Name);
            writer.WriteString(t.Description ?? "");
            writer.WriteInt32((int)t.Type);
        }
        var data = writer.ToArray();

        var request = new SessionUpdateTargetsRequest(data);

        request.MessageId.Should().Be(messageId);
        request.SessionId.Should().Be(sessionId);
        request.Targets.Should().HaveCount(2);
        request.Targets[0].Id.Should().Be(targets[0].Id);
        request.Targets[1].Id.Should().Be(targets[1].Id);
    }

    [Fact]
    public void SerializeCreateResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        var bytes = SessionProtocolSerializer.SerializeCreateResult(messageId, true, sessionId);

        bytes[0].Should().Be((byte)MessageType.SessionCreateResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        bytes[17].Should().Be(1);
        new Guid(bytes.AsSpan(18, 16)).Should().Be(sessionId);
    }

    [Fact]
    public void SerializeLoadResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var session = new SessionDto
        {
            Id = Guid.NewGuid(),
            Name = "Loaded Session",
            Description = "Some desc",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            History = new List<HistoryEntryDto>
            {
                new() { Id = Guid.NewGuid(), Timestamp = DateTime.UtcNow, Command = "ls", Output = "file1", ExitCode = 0 }
            },
            Targets = new List<SessionTargetDto>
            {
                new() { Id = Guid.NewGuid(), Address = "localhost", Port = 22, Name = "SSH", Type = TargetType.Misc }
            }
        };

        var bytes = SessionProtocolSerializer.SerializeLoadResult(messageId, true, session);

        bytes[0].Should().Be((byte)MessageType.SessionLoadResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        bytes[17].Should().Be(1);
        // We could check further bytes if needed, but this covers the code path.
    }

    [Fact]
    public void SerializeLoadResult_Failure_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var bytes = SessionProtocolSerializer.SerializeLoadResult(messageId, false, null);

        bytes[0].Should().Be((byte)MessageType.SessionLoadResult);
        bytes[17].Should().Be(0);
        bytes.Length.Should().Be(18);
    }

    [Fact]
    public void SerializeListResult_ShouldRoundTrip()
    {
        var messageId = Guid.NewGuid();
        var sessions = new List<SessionMetadataDto>
        {
            new() { Id = Guid.NewGuid(), Name = "S1", HistoryCount = 1, TargetCount = 1 },
            new() { Id = Guid.NewGuid(), Name = "S2", HistoryCount = 0, TargetCount = 0 }
        };

        var bytes = SessionProtocolSerializer.SerializeListResult(messageId, sessions, 10);

        bytes[0].Should().Be((byte)MessageType.SessionListResult);
        new Guid(bytes.AsSpan(1, 16)).Should().Be(messageId);
        BitConverter.ToInt32(bytes.AsSpan(17, 4)).Should().Be(2);
        BitConverter.ToInt32(bytes.AsSpan(21, 4)).Should().Be(10);
    }

    [Fact]
    public void IsSessionMessage_ShouldDetectCorrectRange()
    {
        SessionProtocolDeserializer.IsSessionMessage(MessageType.SessionCreate).Should().BeTrue();
        SessionProtocolDeserializer.IsSessionMessage(MessageType.SessionEditTarget).Should().BeTrue();
        SessionProtocolDeserializer.IsSessionMessage(MessageType.ProjectCreate).Should().BeFalse();
    }
}
