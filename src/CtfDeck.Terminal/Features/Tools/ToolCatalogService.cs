using System.Reflection;
using System.Text.Json;
using CtfDeck.Abstractions.Ports.Tools;
using CtfDeck.Contracts.Models.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public class ToolCatalogService : IToolCatalogProvider
{
    private const string ResourceName = "CtfDeck.Terminal.Resources.tool-catalog.json";

    private readonly Lazy<Task<ToolCatalog>> _catalogLoader = new(LoadCatalogAsync);

    public async Task<IReadOnlyCollection<ToolDefinition>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var catalog = await _catalogLoader.Value;
        return catalog.Tools;
    }

    public async Task<ToolDefinition?> GetByIdAsync(string toolId, CancellationToken cancellationToken = default)
    {
        var catalog = await _catalogLoader.Value;
        return catalog.Tools.FirstOrDefault(t => t.Id.Equals(toolId, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<ToolCatalog> LoadCatalogAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        await using var stream = assembly.GetManifestResourceStream(ResourceName)
                                 ?? throw new InvalidOperationException($"Embedded resource '{ResourceName}' not found.");

        using var reader = new StreamReader(stream);
        var json = await reader.ReadToEndAsync();

        var catalog = JsonSerializer.Deserialize<ToolCatalog>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))
                      ?? throw new InvalidOperationException("Unable to deserialize tool catalog.");

        Validate(catalog);
        return catalog;
    }

    private static void Validate(ToolCatalog catalog)
    {
        if (catalog.SchemaVersion <= 0)
            throw new InvalidOperationException("Invalid tool catalog schemaVersion.");

        var duplicates = catalog.Tools
            .GroupBy(t => t.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToArray();

        if (duplicates.Length > 0)
            throw new InvalidOperationException($"Duplicate tool ids: {string.Join(", ", duplicates)}");

        foreach (var tool in catalog.Tools)
        {
            if (string.IsNullOrWhiteSpace(tool.Id))
                throw new InvalidOperationException("Tool id cannot be empty.");

            if (tool.Installers.Count == 0)
                throw new InvalidOperationException($"Tool '{tool.Id}' has no installer.");

            foreach (var installer in tool.Installers)
            {
                if (string.IsNullOrWhiteSpace(installer.Os) ||
                    string.IsNullOrWhiteSpace(installer.Arch) ||
                    string.IsNullOrWhiteSpace(installer.Type) ||
                    string.IsNullOrWhiteSpace(installer.Url) ||
                    string.IsNullOrWhiteSpace(installer.ArchiveType) ||
                    string.IsNullOrWhiteSpace(installer.ExecutableRelativePath) ||
                    string.IsNullOrWhiteSpace(installer.ExecutableName))
                {
                    throw new InvalidOperationException($"Tool '{tool.Id}' contains an invalid installer.");
                }
            }
        }
    }
}
