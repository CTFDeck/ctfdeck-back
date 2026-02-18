using CtfDeck.Contracts.Models.Scripts;

namespace CtfDeck.Abstractions.Ports.Scripts;

public interface ICustomScriptRepository
{
    CustomScriptDto Create(string name, ScriptCategory category, string template);
    List<CustomScriptDto> GetAll();
    bool Update(Guid id, string name, ScriptCategory category, string template);
    bool Delete(Guid id);
}
