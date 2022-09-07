namespace LaunchFfxiv;

public class Config
{
    public string WinePath { get; set; } = "wine";

    public string WinePrefixPath { get; set; } = "~/.wine";

    public string RpcapdPath { get; set; } = "rpcapd";

    public string IinactPath { get; set; } = "WINEPREFIX/drive_c/IINACT.exe";

    public List<string> AutoRun { get; set; } = new();

    public string XlCorePath { get; set; } = string.Empty;

    public bool WineEsync { get; set; } = true;

    public bool WineFsync { get; set; } = true;

    public bool DisableWineLogs { get; set; } = true;

    public bool DisableDxvkLogs { get; set; } = true;

    public List<string> WineDllOverrides { get; set; } = new();

    public bool Debug { get; set; } = false;

    public bool FlatpakLauncher { get; set; } = true;

    public static readonly Config Default = new();
}