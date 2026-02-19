namespace CtfDeck.Contracts.Models.Scripts;

public sealed class CustomScriptDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public ScriptCategory Category { get; set; }
    public string Template { get; set; } = "";
}
