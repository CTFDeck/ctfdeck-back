using CtfDeck.Terminal.Script.Models;
using CtfDeck.Terminal.Session.Data;

namespace CtfDeck.Terminal.Script.Repositories;

public class CustomScriptRepository : ICustomScriptRepository
{
    private readonly SessionDbContext _context;
    private readonly object _lock = new();

    public CustomScriptRepository(SessionDbContext context)
    {
        _context = context;
    }

    public CustomScript Create(string name, ScriptCategory category, string template)
    {
        var script = new CustomScript
        {
            Id = Guid.NewGuid(),
            Name = name,
            Category = category,
            Template = template
        };

        lock (_lock)
        {
            _context.CustomScripts.Insert(script);
        }

        return script;
    }

    public List<CustomScript> GetAll()
    {
        lock (_lock)
        {
            return _context.CustomScripts.FindAll().ToList();
        }
    }

    public bool Update(Guid id, string name, ScriptCategory category, string template)
    {
        lock (_lock)
        {
            var script = _context.CustomScripts.FindById(id);
            if (script == null) return false;

            script.Name = name;
            script.Category = category;
            script.Template = template;
            return _context.CustomScripts.Update(script);
        }
    }

    public bool Delete(Guid id)
    {
        lock (_lock)
        {
            return _context.CustomScripts.Delete(id);
        }
    }
}
