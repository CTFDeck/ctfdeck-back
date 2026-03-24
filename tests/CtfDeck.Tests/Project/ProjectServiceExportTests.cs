using System.Text.Json;
using CtfDeck.Contracts.Models.Projects;
using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Contracts.Models.Sessions;
using CtfDeck.Data.Db;
using CtfDeck.Data.Repositories.Projects;
using CtfDeck.Data.Repositories.Scripts;
using CtfDeck.Data.Repositories.Sessions;
using CtfDeck.Data.Repositories.WriteUps;
using CtfDeck.Data.Repositories.Media;
using CtfDeck.Terminal.Features.Projects;
using FluentAssertions;

namespace CtfDeck.Tests.Project;

public sealed class ProjectServiceExportTests : IDisposable
{
    private readonly CtfDeckDbContext _db;
    private readonly ProjectService _service;
    private readonly SessionRepository _sessionRepo;
    private readonly CustomScriptRepository _scriptRepo;
    private readonly WriteUpRepository _writeUpRepo;
    private readonly MediaRepository _mediaRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly string _tempDir;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ProjectServiceExportTests()
    {
        _db = new CtfDeckDbContext(":memory:");
        _sessionRepo = new SessionRepository(_db);
        _scriptRepo = new CustomScriptRepository(_db);
        _writeUpRepo = new WriteUpRepository(_db);
        _mediaRepo = new MediaRepository(_db);
        _projectRepo = new ProjectRepository(_db);
        _service = new ProjectService(_projectRepo, _sessionRepo, _writeUpRepo, _mediaRepo, _scriptRepo);
        _tempDir = Path.Combine(Path.GetTempPath(), $"ctfdeck-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string TempFile(string name) => Path.Combine(_tempDir, name);

    private (ProjectDto Project, SessionDto Session) SetupProjectWithSession()
    {
        var project = _service.Create("TestProject", "desc");
        var session = _sessionRepo.Create("TestSession");
        _sessionRepo.SetProjectId(session.Id, project.Id);
        _sessionRepo.AddTarget(session.Id, new SessionTargetDto
        {
            Id = Guid.NewGuid(),
            Address = "10.10.10.1",
            Port = 80,
            Name = "web",
            Description = "http",
            Type = TargetType.Web
        });

        // Reload session to get targets
        var loaded = _sessionRepo.GetById(session.Id)!;
        return (project, loaded);
    }

    [Fact]
    public void ExportAll_ShouldIncludeEverything()
    {
        var (project, session) = SetupProjectWithSession();
        var script = _scriptRepo.Create("nmap-scan", ScriptCategory.Discovery, "nmap -sV {{target}}");
        var path = TempFile("all.json");

        _service.ExportToFile(project.Id, path, ExportOptions.All);

        var export = JsonSerializer.Deserialize<ProjectExportDto>(File.ReadAllText(path), JsonOptions)!;
        export.Project.Id.Should().Be(project.Id);
        export.Sessions.Should().HaveCount(1);
        export.Sessions[0].Targets.Should().HaveCount(1);
        export.Scripts.Should().NotBeNull();
        export.Scripts!.Should().ContainSingle(s => s.Id == script.Id);
    }

    [Fact]
    public void ExportWithoutHistory_ShouldHaveEmptyHistory()
    {
        var (project, _) = SetupProjectWithSession();
        var path = TempFile("no-history.json");
        var options = new ExportOptions(IncludeHistory: false);

        _service.ExportToFile(project.Id, path, options);

        var export = JsonSerializer.Deserialize<ProjectExportDto>(File.ReadAllText(path), JsonOptions)!;
        export.Sessions.Should().HaveCount(1);
        export.Sessions[0].History.Should().BeEmpty();
        export.Sessions[0].Targets.Should().HaveCount(1, "targets should still be included");
    }

    [Fact]
    public void ExportWithoutTargets_ShouldHaveEmptyTargets()
    {
        var (project, _) = SetupProjectWithSession();
        var path = TempFile("no-targets.json");
        var options = new ExportOptions(IncludeTargets: false);

        _service.ExportToFile(project.Id, path, options);

        var export = JsonSerializer.Deserialize<ProjectExportDto>(File.ReadAllText(path), JsonOptions)!;
        export.Sessions.Should().HaveCount(1);
        export.Sessions[0].Targets.Should().BeEmpty();
    }

    [Fact]
    public void ExportWithoutWriteUps_ShouldHaveEmptyWriteUpsAndMedia()
    {
        var (project, _) = SetupProjectWithSession();
        var path = TempFile("no-writeups.json");
        var options = new ExportOptions(IncludeWriteUps: false);

        _service.ExportToFile(project.Id, path, options);

        var export = JsonSerializer.Deserialize<ProjectExportDto>(File.ReadAllText(path), JsonOptions)!;
        export.WriteUps.Should().BeEmpty();
        export.Media.Should().BeEmpty();
    }

    [Fact]
    public void ExportWithoutScripts_ShouldHaveNullScripts()
    {
        var (project, _) = SetupProjectWithSession();
        _scriptRepo.Create("test-script", ScriptCategory.Other, "echo hello");
        var path = TempFile("no-scripts.json");
        var options = new ExportOptions(IncludeScripts: false);

        _service.ExportToFile(project.Id, path, options);

        var export = JsonSerializer.Deserialize<ProjectExportDto>(File.ReadAllText(path), JsonOptions)!;
        export.Scripts.Should().BeNull();
    }

    [Fact]
    public void ExportScriptsOnly_ShouldStripSessionDataButKeepScripts()
    {
        var (project, _) = SetupProjectWithSession();
        _scriptRepo.Create("recon", ScriptCategory.Discovery, "recon {{ip}}");
        var path = TempFile("scripts-only.json");
        var options = new ExportOptions(
            IncludeHistory: false,
            IncludeTargets: false,
            IncludeWriteUps: false,
            IncludeMedia: false,
            IncludeScripts: true);

        _service.ExportToFile(project.Id, path, options);

        var export = JsonSerializer.Deserialize<ProjectExportDto>(File.ReadAllText(path), JsonOptions)!;
        export.Sessions[0].History.Should().BeEmpty();
        export.Sessions[0].Targets.Should().BeEmpty();
        export.WriteUps.Should().BeEmpty();
        export.Media.Should().BeEmpty();
        export.Scripts.Should().NotBeNull();
        export.Scripts!.Should().HaveCount(1);
    }

    [Fact]
    public void ImportWithScripts_ShouldCreateScriptsInDb()
    {
        // Setup and export with scripts
        var (project, session) = SetupProjectWithSession();
        var script = _scriptRepo.Create("imported-script", ScriptCategory.Exploit, "payload {{target}}");
        var path = TempFile("with-scripts.json");
        _service.ExportToFile(project.Id, path, ExportOptions.All);

        // Delete everything
        _service.Delete(project.Id);
        _sessionRepo.Delete(session.Id);
        _scriptRepo.Delete(script.Id);
        _scriptRepo.GetAll().Should().BeEmpty();

        // Import
        var importedId = _service.ImportFromFile(path);

        importedId.Should().Be(project.Id);
        var scripts = _scriptRepo.GetAll();
        scripts.Should().ContainSingle(s => s.Id == script.Id && s.Name == "imported-script");
    }

    [Fact]
    public void ImportWithoutScripts_ShouldNotFail()
    {
        // Export without scripts
        var (project, session) = SetupProjectWithSession();
        var path = TempFile("no-scripts-import.json");
        _service.ExportToFile(project.Id, path, new ExportOptions(IncludeScripts: false));

        // Delete project+session
        _service.Delete(project.Id);
        _sessionRepo.Delete(session.Id);

        // Import — should work fine with no scripts in JSON
        var importedId = _service.ImportFromFile(path);
        importedId.Should().Be(project.Id);
    }

    [Fact]
    public void CustomScriptRepository_Insert_ShouldPersist()
    {
        var dto = new CustomScriptDto
        {
            Id = Guid.NewGuid(),
            Name = "test-insert",
            Category = ScriptCategory.Web,
            Template = "curl {{url}}"
        };

        _scriptRepo.Insert(dto);

        var all = _scriptRepo.GetAll();
        all.Should().ContainSingle(s => s.Id == dto.Id && s.Name == "test-insert" && s.Template == "curl {{url}}");
    }
}
