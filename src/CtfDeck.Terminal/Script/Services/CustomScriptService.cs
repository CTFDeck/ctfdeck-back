using CtfDeck.Terminal.Script.Models;
using CtfDeck.Terminal.Script.Repositories;

namespace CtfDeck.Terminal.Script.Services;

public class CustomScriptService
{
    private readonly ICustomScriptRepository _repository;

    public CustomScriptService(ICustomScriptRepository repository)
    {
        _repository = repository;
    }

    public CustomScript Create(string name, ScriptCategory category, string template)
    {
        return _repository.Create(name, category, template);
    }

    public List<CustomScript> GetAll()
    {
        return _repository.GetAll();
    }

    public bool Update(Guid id, string name, ScriptCategory category, string template)
    {
        return _repository.Update(id, name, category, template);
    }

    public bool Delete(Guid id)
    {
        return _repository.Delete(id);
    }
}
