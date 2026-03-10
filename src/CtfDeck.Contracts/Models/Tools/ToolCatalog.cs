namespace CtfDeck.Contracts.Models.Tools;

public class ToolCatalog
{
    public int SchemaVersion { get; set; }
    public List<ToolDefinition> Tools { get; set; } = [];
}
