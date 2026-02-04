namespace CtfDeck.Terminal.Session.Models;

public class SessionTarget
{
    public Guid Id { get; set; }
    public string Address { get; set; } = string.Empty;
    public int? Port { get; set; }
    public string Name { get; set; } = string.Empty;
    public TargetType Type { get; set; }
}
