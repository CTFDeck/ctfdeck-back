namespace CtfDeck.Data.PersistenceModels.Scripts;

public class CustomScript
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ScriptCategory Category { get; set; }
    public string Template { get; set; } = string.Empty;
}
