namespace CtfDeck.Contracts.Models.Sessions;

public sealed class SessionTargetDto
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TargetType Type { get; set; }
}
