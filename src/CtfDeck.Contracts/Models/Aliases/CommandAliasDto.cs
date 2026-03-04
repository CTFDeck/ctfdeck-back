namespace CtfDeck.Contracts.Models.Aliases;

public enum OsKind { Any = 0, Windows = 1, Linux = 2, MacOs = 3 }
public enum ShellKind { Any = 0, Bash = 1, Pwsh = 2 }

public sealed class CommandAliasDto
{
    public Guid Id { get; set; }
    public string From { get; set; } = "";
    public string To { get; set; } = "";
    public OsKind Os { get; set; } = OsKind.Any;
    public ShellKind Shell { get; set; } = ShellKind.Any;
    public bool IsDefault { get; set; }
    public DateTime UpdatedAt { get; set; }
}
