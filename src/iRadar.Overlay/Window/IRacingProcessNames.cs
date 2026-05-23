namespace iRadar.Overlay.Window;

// Process names for the iRacing executable across rendering modes.
// Process.ProcessName returns the executable name WITHOUT the .exe extension.
public static class IRacingProcessNames
{
    public const string Dx11 = "iRacingSim64DX11";
    public const string Dx12 = "iRacingSim64DX12";

    public static IReadOnlyCollection<string> All { get; } = new[] { Dx11, Dx12 };
}
