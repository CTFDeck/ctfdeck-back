using LiteDB;
using SessionModel = CtfDeck.Terminal.Session.Models.Session;
using CtfDeck.Terminal.Session.Models;

namespace CtfDeck.Terminal.Session.Data;

public class SessionDbContext : IDisposable
{
    private static bool _mapperConfigured;
    private static readonly object _mapperLock = new();

    private readonly LiteDatabase _database;
    private readonly ILiteCollection<SessionModel> _sessions;
    private bool _disposed;

    public SessionDbContext(string? databasePath = null)
    {
        // Use in-memory database if path is ":memory:" or null for tests
        var connectionString = databasePath == ":memory:"
            ? "Filename=:memory:;Mode=Memory;Cache=Shared"
            : $"Filename={databasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ctfdeck_sessions.db")}";

        ConfigureBsonMapper();
        _database = new LiteDatabase(connectionString);
        _sessions = _database.GetCollection<SessionModel>("sessions");
        _sessions.EnsureIndex(x => x.Id, unique: true);
        _sessions.EnsureIndex(x => x.Name);
    }

    public ILiteCollection<SessionModel> Sessions => _sessions;

    private static void ConfigureBsonMapper()
    {
        if (_mapperConfigured) return;

        lock (_mapperLock)
        {
            if (_mapperConfigured) return;

            BsonMapper.Global.EnumAsInteger = true;

            BsonMapper.Global.Entity<SessionModel>()
                .Id(x => x.Id);

            BsonMapper.Global.Entity<HistoryEntry>()
                .Id(x => x.Id);

            BsonMapper.Global.Entity<SessionTarget>()
                .Id(x => x.Id);

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
