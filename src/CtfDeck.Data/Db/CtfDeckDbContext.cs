using LiteDB;
using CtfDeck.Data.PersistenceModels.Sessions;
using CtfDeck.Data.PersistenceModels.Scripts;
using CtfDeck.Data.PersistenceModels.WriteUps;
using CtfDeck.Data.PersistenceModels.Media;
using CtfDeck.Data.PersistenceModels.Projects;
using CtfDeck.Data.PersistenceModels.Aliases;
namespace CtfDeck.Data.Db;

public sealed class CtfDeckDbContext : IDisposable
{
    private static bool _mapperConfigured;
    private static readonly object _mapperLock = new();

    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Session> _sessions;
    private readonly ILiteCollection<CustomScript> _customScripts;
    private readonly ILiteCollection<WriteUp> _writeUps;
    private readonly ILiteCollection<Media> _media;
    private readonly ILiteCollection<Project> _projects;
    private bool _disposed;
    private readonly ILiteCollection<CommandAlias> _aliases;


    public CtfDeckDbContext(string? databasePath = null)
    {
        // Use in-memory database if path is ":memory:" or null for tests
        var connectionString = databasePath == ":memory:"
            ? "Filename=:memory:;Mode=Memory;Cache=Shared"
            : $"Filename={databasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ctfdeck.db")}";

        ConfigureBsonMapper();

        _database = new LiteDatabase(connectionString);

        _sessions = _database.GetCollection<Session>("sessions");
        _sessions.EnsureIndex(x => x.Id, unique: true);
        _sessions.EnsureIndex(x => x.Name);

        _customScripts = _database.GetCollection<CustomScript>("customscripts");
        _customScripts.EnsureIndex(x => x.Id, unique: true);

        _writeUps = _database.GetCollection<WriteUp>("writeups");
        _writeUps.EnsureIndex(x => x.Id, unique: true);
        _writeUps.EnsureIndex(x => x.SessionId);

        _media = _database.GetCollection<PersistenceModels.Media.Media>("media");
        _media.EnsureIndex(x => x.Id, unique: true);
        _media.EnsureIndex(x => x.FileName);

        _projects = _database.GetCollection<Project>("projects");
        _projects.EnsureIndex(x => x.Id, unique: true);
        _projects.EnsureIndex(x => x.Name);

        _sessions.EnsureIndex(x => x.ProjectId);
        _writeUps.EnsureIndex(x => x.FolderId);

        _aliases = _database.GetCollection<CommandAlias>("aliases");
        _aliases.EnsureIndex(x => x.Id, unique: true);
        _aliases.EnsureIndex(x => x.From);
    }

    public ILiteCollection<Session> Sessions => _sessions;
    public ILiteCollection<CustomScript> CustomScripts => _customScripts;
    public ILiteCollection<WriteUp> WriteUps => _writeUps;
    public ILiteCollection<Media> Media => _media;
    public ILiteCollection<Project> Projects => _projects;
    public ILiteCollection<CommandAlias> Aliases => _aliases;

    private static void ConfigureBsonMapper()
    {
        if (_mapperConfigured) return;

        lock (_mapperLock)
        {
            if (_mapperConfigured) return;

            BsonMapper.Global.EnumAsInteger = true;

            BsonMapper.Global.Entity<Session>().Id(x => x.Id);
            BsonMapper.Global.Entity<HistoryEntry>().Id(x => x.Id);
            BsonMapper.Global.Entity<SessionTarget>().Id(x => x.Id);
            BsonMapper.Global.Entity<CustomScript>().Id(x => x.Id);
            BsonMapper.Global.Entity<WriteUp>().Id(x => x.Id);
            BsonMapper.Global.Entity<Media>().Id(x => x.Id);
            BsonMapper.Global.Entity<Project>().Id(x => x.Id);
            BsonMapper.Global.Entity<ProjectFolder>().Id(x => x.Id);
            BsonMapper.Global.Entity<CommandAlias>().Id(x => x.Id);

            _mapperConfigured = true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _database.Dispose();
        _disposed = true;
    }
}
