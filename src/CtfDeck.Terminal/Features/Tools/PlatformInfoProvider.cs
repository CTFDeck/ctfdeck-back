using System.Runtime.InteropServices;
using CtfDeck.Abstractions.Ports.Tools;

namespace CtfDeck.Terminal.Features.Tools;

public sealed class PlatformInfoProvider : IPlatformInfoProvider
{
    public string GetOs()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return "windows";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return "macos";
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return "linux";
        throw new PlatformNotSupportedException("Unsupported OS.");
    }

    public string GetArch()
    {
        return RuntimeInformation.OSArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.Arm64 => "arm64",
            _ => throw new PlatformNotSupportedException($"Unsupported architecture: {RuntimeInformation.OSArchitecture}")
        };
    }
}
