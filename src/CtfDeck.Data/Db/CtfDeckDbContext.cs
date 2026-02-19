using LiteDB;
using CtfDeck.Data.PersistenceModels.Sessions;
using CtfDeck.Data.PersistenceModels.Scripts;

namespace CtfDeck.Data.Db;

public sealed class CtfDeckDbContext : IDisposable
{
    private static bool _mapperConfigured;
    private static readonly object _mapperLock = new();

    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Session> _sessions;
    private readonly ILiteCollection<CustomScript> _customScripts;
    private bool _disposed;

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
    }

    public ILiteCollection<Session> Sessions => _sessions;
    public ILiteCollection<CustomScript> CustomScripts => _customScripts;

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
