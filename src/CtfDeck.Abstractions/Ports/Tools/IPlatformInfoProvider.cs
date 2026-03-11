namespace CtfDeck.Abstractions.Ports.Tools;

public interface IPlatformInfoProvider
{
    string GetOs();
    string GetArch();
}
