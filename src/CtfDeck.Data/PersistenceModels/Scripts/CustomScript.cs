namespace CtfDeck.Data.PersistenceModels.Scripts;

public enum ScriptCategory
{
    Discovery = 0,
    Web = 1,
    ReverseShell = 2,
    Exploit = 3,
    Other = 4
}

public class CustomScript
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ScriptCategory Category { get; set; }
    public string Template { get; set; } = string.Empty;
}
