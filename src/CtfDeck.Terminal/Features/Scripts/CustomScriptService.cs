using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Abstractions.Ports.Scripts;

namespace CtfDeck.Terminal.Features.Scripts;

public class CustomScriptService
{
    private readonly ICustomScriptRepository _repository;

    public CustomScriptService(ICustomScriptRepository repository)
    {
        _repository = repository;
    }

    public CustomScriptDto Create(string name, ScriptCategory category, string template)
        => _repository.Create(name, category, template);

    public List<CustomScriptDto> GetAll()
        => _repository.GetAll();

    public bool Update(Guid id, string name, ScriptCategory category, string template)
        => _repository.Update(id, name, category, template);

    public bool Delete(Guid id)
        => _repository.Delete(id);
}
