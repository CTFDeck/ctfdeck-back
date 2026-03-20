using CtfDeck.Contracts.Models.Aliases;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Contracts.Models.WriteUps;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Aliases;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Data.Repositories.WriteUps;
using FluentAssertions;

namespace CtfDeck.Tests.Data;

public sealed class SessionRepositoryBranchTests : IDisposable
{
    private readonly CtfDeckDbContext _db = new(":memory:");
    private readonly SessionRepository _repo;

    public SessionRepositoryBranchTests()
    {
        _repo = new SessionRepository(_db);
    }

    [Fact]
    public void GetByFolderId_ShouldReturnOnlyMatchingSessions()
    {
        var folderA = Guid.NewGuid();
        var folderB = Guid.NewGuid();
        var s1 = _repo.Create("A");
        var s2 = _repo.Create("B");
        _repo.SetFolderId(s1.Id, folderA);
        _repo.SetFolderId(s2.Id, folderB);

        var result = _repo.GetByFolderId(folderA).ToList();

        result.Should().ContainSingle();
        result[0].Id.Should().Be(s1.Id);
    }

    [Fact]
    public void UpdateTargets_WhenSessionDoesNotExist_ShouldBeNoOp()
    {
        var missingId = Guid.NewGuid();
        var action = () => _repo.UpdateTargets(missingId, [new SessionTargetDto { Name = "x", Address = "1.1.1.1" }]);

        action.Should().NotThrow();
    }

    [Fact]
    public void SetFolderId_WhenSessionMissing_ShouldReturnFalse()
    {
        var ok = _repo.SetFolderId(Guid.NewGuid(), Guid.NewGuid());

        ok.Should().BeFalse();
    }

    [Fact]
    public void ClearProjectId_ShouldUnsetOnlyTargetProject()
    {
        var projectA = Guid.NewGuid();
        var projectB = Guid.NewGuid();
        var s1 = _repo.Create("A");
        var s2 = _repo.Create("B");
        _repo.SetProjectId(s1.Id, projectA);
        _repo.SetProjectId(s2.Id, projectB);

        _repo.ClearProjectId(projectA);

        _repo.GetById(s1.Id)!.ProjectId.Should().BeNull();
        _repo.GetById(s2.Id)!.ProjectId.Should().Be(projectB);
    }

    [Fact]
    public void GetByProjectId_WithOffsetLimit_ShouldPaginateAndKeepTotalCount()
    {
        var projectId = Guid.NewGuid();

        var sessions = new[]
        {
            MakeSession("S1", DateTime.UtcNow.AddMinutes(-3), projectId),
            MakeSession("S2", DateTime.UtcNow.AddMinutes(-2), projectId),
            MakeSession("S3", DateTime.UtcNow.AddMinutes(-1), projectId)
        };

        foreach (var s in sessions)
            _repo.Insert(s);

        var (items, totalCount) = _repo.GetByProjectId(projectId, offset: 1, limit: 1);
        var list = items.ToList();

        totalCount.Should().Be(3);
        list.Should().ContainSingle();
        list[0].Name.Should().Be("S2");
    }

    private static SessionDto MakeSession(string name, DateTime createdAt, Guid projectId)
    {
        return new SessionDto
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = "",
            CreatedAt = createdAt,
            UpdatedAt = createdAt,
            ProjectId = projectId,
            FolderId = null,
            History = [],
            Targets = []
        };
    }

    public void Dispose() => _db.Dispose();
}

public sealed class WriteUpRepositoryBranchTests : IDisposable
{
    private readonly CtfDeckDbContext _db = new(":memory:");
    private readonly WriteUpRepository _repo;

    public WriteUpRepositoryBranchTests()
    {
        _repo = new WriteUpRepository(_db);
    }

    [Fact]
    public void SetFolderId_WhenWriteUpMissing_ShouldReturnFalse()
    {
        var ok = _repo.SetFolderId(Guid.NewGuid(), Guid.NewGuid());
        ok.Should().BeFalse();
    }

    [Fact]
    public void ClearProjectId_ShouldUnsetProjectAndFolder()
    {
        var projectId = Guid.NewGuid();
        var folderId = Guid.NewGuid();
        var wu = _repo.Create(null, "WU");
        _repo.SetProjectAndFolderId(wu.Id, projectId, folderId);

        _repo.ClearProjectId(projectId);

        var loaded = _repo.GetById(wu.Id)!;
        loaded.ProjectId.Should().BeNull();
        loaded.FolderId.Should().BeNull();
    }

    [Fact]
    public void GetAllMetadata_UnassignedOnly_ShouldReturnOnlyProjectNull()
    {
        var projectId = Guid.NewGuid();
        var assigned = MakeWriteUp("assigned", DateTime.UtcNow.AddMinutes(-2), projectId);
        var unassigned = MakeWriteUp("unassigned", DateTime.UtcNow.AddMinutes(-1), null);
        _repo.Insert(assigned);
        _repo.Insert(unassigned);

        var (items, totalCount) = _repo.GetAllMetadata(unassignedOnly: true);
        var list = items.ToList();

        totalCount.Should().Be(1);
        list.Should().ContainSingle();
        list[0].Name.Should().Be("unassigned");
    }

    [Fact]
    public void Insert_ThenGetByFolderId_ShouldReturnInsertedData()
    {
        var folderId = Guid.NewGuid();
        var writeUp = MakeWriteUp("Inserted", DateTime.UtcNow, null);
        writeUp.FolderId = folderId;
        _repo.Insert(writeUp);

        var (items, totalCount) = _repo.GetByFolderId(folderId);
        var list = items.ToList();

        totalCount.Should().Be(1);
        list.Should().ContainSingle();
        list[0].Id.Should().Be(writeUp.Id);
    }

    private static WriteUpDto MakeWriteUp(string name, DateTime createdAt, Guid? projectId)
    {
        return new WriteUpDto
        {
            Id = Guid.NewGuid(),
            SessionId = null,
            ProjectId = projectId,
            FolderId = null,
            Name = name,
            Content = "",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public void Dispose() => _db.Dispose();
}

public sealed class CommandAliasRepositoryBranchTests : IDisposable
{
    private readonly CtfDeckDbContext _db = new(":memory:");
    private readonly CommandAliasRepository _repo;

    public CommandAliasRepositoryBranchTests()
    {
        _repo = new CommandAliasRepository(_db);
    }

    [Fact]
    public void EnsureSeededDefaults_ShouldBeIdempotent()
    {
        _repo.EnsureSeededDefaults();
        var firstCount = _repo.GetAll().Count;

        _repo.EnsureSeededDefaults();
        var secondCount = _repo.GetAll().Count;

        secondCount.Should().Be(firstCount);
    }

    [Fact]
    public void Upsert_WithEmptyId_ShouldAssignId_AndPersist()
    {
        var dto = new CommandAliasDto
        {
            Id = Guid.Empty,
            From = "ll",
            To = "ls -lah",
            Os = OsKind.Linux,
            Shell = ShellKind.Bash,
            IsDefault = false
        };

        var inserted = _repo.Upsert(dto);
        var all = _repo.GetAll();

        inserted.Id.Should().NotBe(Guid.Empty);
        all.Should().ContainSingle(x => x.Id == inserted.Id && x.From == "ll" && x.To == "ls -lah");
    }

    [Fact]
    public void GetFor_ShouldPreferMostSpecificAlias()
    {
        _repo.Upsert(new CommandAliasDto
        {
            From = "ls",
            To = "alias-any",
            Os = OsKind.Any,
            Shell = ShellKind.Any,
            IsDefault = false
        });
        _repo.Upsert(new CommandAliasDto
        {
            From = "ls",
            To = "alias-linux-bash",
            Os = OsKind.Linux,
            Shell = ShellKind.Bash,
            IsDefault = false
        });

        var resolved = _repo.GetFor(OsKind.Linux, ShellKind.Bash);
        resolved.Should().Contain(x => x.From == "ls" && x.To == "alias-linux-bash");
    }

    [Fact]
    public void Delete_ShouldReturnFalse_WhenIdMissing()
    {
        var ok = _repo.Delete(Guid.NewGuid());
        ok.Should().BeFalse();
    }

    public void Dispose() => _db.Dispose();
}
