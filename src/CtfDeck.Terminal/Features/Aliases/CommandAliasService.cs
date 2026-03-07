using CtfDeck.Abstractions.Ports.Aliases;
using CtfDeck.Contracts.Models.Aliases;

namespace CtfDeck.Terminal.Features.Aliases;

public sealed class CommandAliasService
{
    private readonly ICommandAliasRepository _repo;

    public CommandAliasService(ICommandAliasRepository repo)
    {
        _repo = repo;
        _repo.EnsureSeededDefaults();
    }

    public string Apply(string command, OsKind os, ShellKind shell)
    {
        var trimmed = command.TrimStart();
        if (string.IsNullOrWhiteSpace(trimmed)) return command;

        var firstSpace = trimmed.IndexOf(' ');
        var verb = firstSpace < 0 ? trimmed : trimmed[..firstSpace];
        var rest = firstSpace < 0 ? "" : trimmed[firstSpace..];

        var aliases = _repo.GetFor(os, shell);
        var match = aliases.FirstOrDefault(a => verb.Equals(a.From, StringComparison.OrdinalIgnoreCase));
        if (match == null) return command;

        return match.To + rest;
    }
}
