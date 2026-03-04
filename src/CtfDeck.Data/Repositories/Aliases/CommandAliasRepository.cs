using CtfDeck.Abstractions.Ports.Aliases;
using CtfDeck.Contracts.Models.Aliases;
using CtfDeck.Data.Db;
using CtfDeck.Data.PersistenceModels.Aliases;

namespace CtfDeck.Data.Repositories.Aliases;

public sealed class CommandAliasRepository : ICommandAliasRepository
{
    private readonly CtfDeckDbContext _db;
    private readonly object _lock = new();

    public CommandAliasRepository(CtfDeckDbContext db) => _db = db;

    public void EnsureSeededDefaults()
    {
        lock (_lock)
        {
            foreach (var a in DefaultAliases())
            {
                var existing = _db.Aliases.FindOne(x =>
                    x.IsDefault &&
                    x.From.Equals(a.From, StringComparison.OrdinalIgnoreCase) &&
                    x.Os == a.Os &&
                    x.Shell == a.Shell);

                if (existing is null)
                {
                    _db.Aliases.Insert(a);
                }
                else
                {
                    existing.To = a.To;
                    existing.UpdatedAt = DateTime.UtcNow;
                    _db.Aliases.Update(existing);
                }
            }
        }
    }

    public List<CommandAliasDto> GetAll()
    {
        lock (_lock) { return _db.Aliases.FindAll().Select(ToDto).ToList(); }
    }

    public List<CommandAliasDto> GetFor(OsKind os, ShellKind shell)
    {
        lock (_lock)
        {
            var all = _db.Aliases.Find(x =>
                    (x.Os == OsKind.Any || x.Os == os) &&
                    (x.Shell == ShellKind.Any || x.Shell == shell))
                .ToList();

            return all
                .GroupBy(x => x.From, StringComparer.OrdinalIgnoreCase)
                .Select(g => g
                    .OrderByDescending(x => Score(x, os, shell))
                    .First())
                .Select(ToDto)
                .ToList();
        }
    }

    public CommandAliasDto Upsert(CommandAliasDto dto)
    {
        var model = FromDto(dto);
        model.UpdatedAt = DateTime.UtcNow;
        if (model.Id == Guid.Empty) model.Id = Guid.NewGuid();

        lock (_lock) { _db.Aliases.Upsert(model); }
        return ToDto(model);
    }

    public bool Delete(Guid id)
    {
        lock (_lock) { return _db.Aliases.Delete(id); }
    }

    private static int Score(CommandAlias a, OsKind os, ShellKind shell)
        => (a.Os == os ? 2 : a.Os == OsKind.Any ? 1 : 0) +
           (a.Shell == shell ? 2 : a.Shell == ShellKind.Any ? 1 : 0);

    private static CommandAliasDto ToDto(CommandAlias a) => new()
    {
        Id = a.Id,
        From = a.From,
        To = a.To,
        Os = a.Os,
        Shell = a.Shell,
        IsDefault = a.IsDefault,
        UpdatedAt = a.UpdatedAt
    };

    private static CommandAlias FromDto(CommandAliasDto d) => new()
    {
        Id = d.Id,
        From = d.From,
        To = d.To,
        Os = d.Os,
        Shell = d.Shell,
        IsDefault = d.IsDefault,
        UpdatedAt = d.UpdatedAt
    };

    private static IEnumerable<CommandAlias> DefaultAliases()
    {
        yield return New("ls", "ls --color=auto", OsKind.Linux, ShellKind.Bash);
        yield return New("ls", "ls -G", OsKind.MacOs, ShellKind.Bash);

        yield return New("dir", "ls --color=auto", OsKind.Linux, ShellKind.Bash);
        yield return New("dir", "ls -G", OsKind.MacOs, ShellKind.Bash);

        yield return New("cat", "cat", OsKind.Any, ShellKind.Bash);
        yield return New("type", "cat", OsKind.Any, ShellKind.Bash);

        yield return New("clear", "clear", OsKind.Any, ShellKind.Bash);
        yield return New("cls", "clear", OsKind.Any, ShellKind.Bash);

        yield return New("ls", "dir", OsKind.Windows, ShellKind.Pwsh);
        yield return New("dir", "dir", OsKind.Windows, ShellKind.Pwsh);

        yield return New("cat", "type", OsKind.Windows, ShellKind.Pwsh);
        yield return New("type", "type", OsKind.Windows, ShellKind.Pwsh);

        yield return New("clear", "cls", OsKind.Windows, ShellKind.Pwsh);
        yield return New("cls", "cls", OsKind.Windows, ShellKind.Pwsh);

        yield return New("which", "where", OsKind.Windows, ShellKind.Pwsh);
    }

    private static CommandAlias New(string from, string to, OsKind os, ShellKind shell) => new()
    {
        Id = Guid.NewGuid(),
        From = from,
        To = to,
        Os = os,
        Shell = shell,
        IsDefault = true,
        UpdatedAt = DateTime.UtcNow
    };
}
