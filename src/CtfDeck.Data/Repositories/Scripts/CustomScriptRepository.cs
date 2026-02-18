using ContractCategory = CtfDeck.Contracts.Models.Scripts.ScriptCategory;
using CtfDeck.Abstractions.Ports.Scripts;
using CtfDeck.Contracts.Models.Scripts;
using CtfDeck.Data.Db;
using CtfDeck.Data.PersistenceModels.Scripts;


namespace CtfDeck.Data.Repositories.Scripts;

public sealed class CustomScriptRepository : ICustomScriptRepository
{
    private readonly CtfDeckDbContext _context;
    private readonly object _lock = new();

    public CustomScriptRepository(CtfDeckDbContext context)
    {
        _context = context;
    }

    public CustomScriptDto Create(string name, ContractCategory category, string template)
    {
        var model = new CustomScript
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = (CtfDeck.Data.PersistenceModels.Scripts.ScriptCategory)category,
            Template = template
        };

        lock (_lock)
        {
            _context.CustomScripts.Insert(model);
        }

        return ToDto(model);
    }

    public List<CustomScriptDto> GetAll()
    {
        lock (_lock)
        {
            return _context.CustomScripts.FindAll().Select(ToDto).ToList();
        }
    }

    public bool Update(Guid id, string name, ContractCategory category, string template)
    {
        lock (_lock)
        {
            var model = _context.CustomScripts.FindById(id);
            if (model == null) return false;

            model.Name = name;
            model.Category = (CtfDeck.Data.PersistenceModels.Scripts.ScriptCategory)category;
            model.Template = template;

            return _context.CustomScripts.Update(model);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.CustomScripts.Delete(id);
        }
    }

    private static CustomScriptDto ToDto(CustomScript m) => new()
    {
        Id = m.Id,
        Name = m.Name,
        Category = (ContractCategory)m.Category,
        Template = m.Template
    };
}