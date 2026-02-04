using LiteDB;
using CtfDeck.Terminal.Session.Models;

namespace CtfDeck.Terminal.Session.Data;

public class SessionDbContext : IDisposable
{
    private readonly LiteDatabase _database;
    private readonly ILiteCollection<Models.Session> _sessions;
    private bool _disposed;

    public SessionDbContext(string? databasePath = null)
    {
        // Use in-memory database if path is ":memory:" or null for tests
        var connectionString = databasePath == ":memory:"
            ? "Filename=:memory:;Mode=Memory;Cache=Shared"
            : $"Filename={databasePath ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ctfdeck_sessions.db")}";

        _database = new LiteDatabase(connectionString);
        ConfigureBsonMapper();
        _sessions = _database.GetCollection<Models.Session>("sessions");
        _sessions.EnsureIndex(x => x.Id, unique: true);
        _sessions.EnsureIndex(x => x.Name);
    }

    public ILiteCollection<Models.Session> Sessions => _sessions;

    private void ConfigureBsonMapper()
    {
        BsonMapper.Global.EnumAsInteger = true;

        BsonMapper.Global.Entity<Models.Session>()
            .Id(x => x.Id);

        BsonMapper.Global.Entity<HistoryEntry>()
            .Id(x => x.Id);

        BsonMapper.Global.Entity<SessionTarget>()
            .Id(x => x.Id);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _database.Dispose();
        _disposed = true;
    }
}
