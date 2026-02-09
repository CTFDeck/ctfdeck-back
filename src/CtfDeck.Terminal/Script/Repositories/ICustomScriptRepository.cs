using CtfDeck.Terminal.Script.Models;

namespace CtfDeck.Terminal.Script.Repositories;

public interface ICustomScriptRepository
{
    CustomScript Create(string name, ScriptCategory category, string template);
    List<CustomScript> GetAll();
    bool Update(Guid id, string name, ScriptCategory category, string template);
    bool Delete(Guid id);
}
