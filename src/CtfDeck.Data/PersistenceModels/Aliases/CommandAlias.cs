using CtfDeck.Contracts.Models.Aliases;

namespace CtfDeck.Data.PersistenceModels.Aliases;

public sealed class CommandAlias
{
    public Guid Id { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public OsKind Os { get; set; }
    public ShellKind Shell { get; set; }
    public bool IsDefault { get; set; }
    public DateTime UpdatedAt { get; set; }
}
