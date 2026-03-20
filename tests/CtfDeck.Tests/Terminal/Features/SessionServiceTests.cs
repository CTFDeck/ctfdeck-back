using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Terminal.Features.Sessions;
using FluentAssertions;

namespace CtfDeck.Tests.Terminal.Features;

public class SessionServiceTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly SessionService _service;
    private readonly SessionRepository _repo;

    public SessionServiceTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        _repo = new SessionRepository(_db);
        _service = new SessionService(_repo);
    }

    public void Dispose()
    {
        _db.Dispose();
    }

    [Fact]
    public void AddHistoryEntry_ShouldTruncateLargeOutput()
    {
        var session = _service.Create("Test Session");
        var largeOutput = new string('a', SessionService.MaxOutputBytes + 1000);
        
        _service.AddHistoryEntry(session.Id, "echo large", largeOutput, 0, "/tmp");
        
        var updatedSession = _service.GetById(session.Id);
        var entry = updatedSession!.History.First();
        
        entry.Command.Should().Be("echo large");
        entry.ExitCode.Should().Be(0);
        entry.WorkingDirectory.Should().Be("/tmp");
        entry.Output.Should().EndWith(SessionService.TruncatedSuffix);
        System.Text.Encoding.UTF8.GetByteCount(entry.Output).Should().BeLessThanOrEqualTo(SessionService.MaxOutputBytes);
    }

    [Fact]
    public void AddHistoryEntry_ShouldNotTruncateSmallOutput()
    {
        var session = _service.Create("Test Session");
        
        _service.AddHistoryEntry(session.Id, "echo small", "small", 0, "/tmp");
        
        var updatedSession = _service.GetById(session.Id);
        var entry = updatedSession!.History.First();
        
        entry.Output.Should().Be("small");
    }
    
    [Fact]
    public void AutoFillDescription_ShouldFillDescriptionIfEmpty()
    {
        var session = _service.Create("Test Session");
        
        var target = new SessionTargetDto
        {
            Id = Guid.NewGuid(),
            Name = "Web",
            Address = "10.10.10.10",
            Port = 80,
            Type = TargetType.Web,
            Description = ""
        };
        
        var addedTarget = _service.AddTarget(session.Id, target);
        
        addedTarget!.Description.Should().Be("Web 10.10.10.10:80");
    }

    [Fact]
    public void AutoFillDescription_ShouldNotFillDescriptionIfNotEmpty()
    {
        var session = _service.Create("Test Session");
        
        var target = new SessionTargetDto
        {
            Id = Guid.NewGuid(),
            Name = "Web",
            Address = "10.10.10.10",
            Port = 80,
            Type = TargetType.Web,
            Description = "Custom Desc"
        };
        
        var addedTarget = _service.AddTarget(session.Id, target);
        
        addedTarget!.Description.Should().Be("Custom Desc");
    }
    
    [Fact]
    public void UpdateTarget_ShouldAutoFillDescriptionIfEmpty()
    {
        var session = _service.Create("Test Session");
        
        var target = new SessionTargetDto
        {
            Id = Guid.NewGuid(),
            Name = "Web",
            Address = "10.10.10.10",
            Type = TargetType.Web,
            Description = "Initial"
        };
        
        _service.AddTarget(session.Id, target);
        
        target.Description = "";
        target.Port = 443;
        _service.UpdateTarget(session.Id, target);
        
        var updatedSession = _service.GetById(session.Id);
        updatedSession!.Targets.First().Description.Should().Be("Web 10.10.10.10:443");
    }
}
