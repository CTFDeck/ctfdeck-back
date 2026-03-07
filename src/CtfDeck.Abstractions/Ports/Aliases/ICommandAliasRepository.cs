using CtfDeck.Contracts.Models.Aliases;

namespace CtfDeck.Abstractions.Ports.Aliases;

public interface ICommandAliasRepository
{
    void EnsureSeededDefaults();

    List<CommandAliasDto> GetAll();
    List<CommandAliasDto> GetFor(OsKind os, ShellKind shell);

    CommandAliasDto Upsert(CommandAliasDto dto);
    bool Delete(Guid id);
}
