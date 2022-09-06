﻿namespace LaunchFfxiv;

using System.Diagnostics;
using System.Net;
using System.Reflection;

using Newtonsoft.Json;

public class Program
{
    private const string KCfgFile = "Launcher.json";
    private static Config? config;

    public static Config Config => config ??= GetConfig();

    public static void Main(string[] args)
    {
        // This is long and I hate it and would love a better, simpler way of handling this
        if (Config == Config.Default || args.Any(s => s == "--setup"))
        {
            string? read = null;
            Log.Warn("Startup", "Default config detected, preforming first time setup.");
            Console.WriteLine("Enter WINE binary path [wine]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
                Config.WinePath = read;

            Console.WriteLine("Enter WINEPREFIX path [~/.wine]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
                Config.WinePrefixPath = read;
            
            Console.WriteLine("Enter RPCAPD path [rpcapd]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
                Config.RpcapdPath = read;
            
            Console.WriteLine("Enter IINACT path [~/IINACT.exe]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
                Config.IinactPath = read;
            
            Console.WriteLine("Enter XlCore path [~/.wine/drive_c/users/$(whoami)/AppData/Local/XIVLauncher/XIVLauncher.exe]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
                Config.XlCorePath = read;

            Console.WriteLine("Enter DotnetBundle extract Dir [/tmp]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
                Config.DotnetBundlePath = read;
            
            Console.WriteLine("Use ESYNC? [Y/n]: ");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
            {
                switch (read.ToLower())
                {
                    case "y":
                    case "yes":
                        Config.WineEsync = true;
                        break;
                    case "n":
                    case "no":
                        Config.WineEsync = false;
                        break;
                }
            }
            
            Console.WriteLine("Use FSYNC? [Y/n]: ");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
            {
                switch (read.ToLower())
                {
                    case "y":
                    case "yes":
                        Config.WineFsync = true;
                        break;
                    case "n":
                    case "no":
                        Config.WineFsync = false;
                        break;
                }
            }

            Console.WriteLine("Disable WINE logs? [Y/n]: ");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
            {
                switch (read.ToLower())
                {
                    case "y":
                    case "yes":
                        Config.DisableWineLogs = true;
                        break;
                    case "n":
                    case "no":
                        Config.DisableWineLogs = false;
                        break;
                }
            }

            Console.WriteLine("Disable DXVK logs? [Y/n]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
            {
                switch (read.ToLower())
                {
                    case "y":
                    case "yes":
                        Config.DisableDxvkLogs = true;
                        break;
                    case "n":
                    case "no":
                        Config.DisableDxvkLogs = false;
                        break;
                }
            }
            
            Console.WriteLine("Are there any WINE DLL overrides you wish to use? (Define them all in a space-seperated list) (use 'none' to remove the default setting) [wpcap=n]:");
            read = Console.ReadLine();
            if (!string.IsNullOrEmpty(read))
            {
                Config.WineDllOverrides.Clear();
                if (read != "none")
                {
                    string[] split = read.Split(' ');
                    foreach (string s in split)
                        Config.WineDllOverrides.Add(s);
                }
            }
            
            Log.Info("Setup", "First time setup complete. Saving config...");

            MakeFilepathsUsable(); // We can't use ~/ in the actual file paths we use, so we need to replace ~/ with /home/Username/ instead.

            SaveConfig();
            Log.Info("Setup", "Config saved.");
        }
        
        StartProcesses();
    }

    private static void StartProcesses()
    {
        Log.Info("Startup", "Starting XLCore");
        if (Config.XlCorePath.EndsWith(".exe")) // Determine if we are using the windows version of XIVLauncher inside of WINE, or the native launcher
            Process.Start(SetupWineProcessInfo(Config.XlCorePath));
        else // If we're using the native launcher, we want to make sure our WINE usage matches the launcher's to make everything work
        {
            string launcherConfig = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".xlcore"), "launcher.ini"); 
            if (!File.Exists(launcherConfig))
            {
                Log.Error("Startup - XLCORE", $"Native XLCore detected, but unable to load XLCore Launcher ini file from {launcherConfig}. Please run XLCore once first to generate it's config.");
                return;
            }

            string[] read = File.ReadAllLines(launcherConfig);
            bool changedWinePath = false;
            foreach (string s in read)
            {
                switch (s)
                {
                    case {} when s.StartsWith("ESyncEnabled"): // Set our Esync usage to match the launcher
                        Config.WineEsync = s.EndsWith("true");
                        break;
                    case {} when s.StartsWith("FSyncEnabled"): // Set out Fsync usage to match the launcher
                        Config.WineFsync = s.EndsWith("true");
                        break;
                    case { } when s.StartsWith("WineStartupType") && s.EndsWith("Managed"): // If the launcher is managing wine for the game client, we need to change our wine binary path to be the same as what's used by the launcher
                        foreach (string dir in Directory.GetDirectories(Path.Combine(Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".xlcore"), "compatibilitytool"), "beta"), "wine*"))
                        {
                            Config.WinePath = Path.Combine(Path.Combine(dir, "bin"), "wine");
                            changedWinePath = true;
                            Log.Info("Startup - XLCORE", Config.WinePath);
                            break;
                        }
                        break;
                    case { } when s.StartsWith("WineBinaryPath") && !changedWinePath: // If we haven't changed the wine path because it's managed by launcher, we need to set our wine path to the custom path defined in launcher config.
                        string path = Path.Combine(s.Substring(s.IndexOf('=') + 1), "wine");
                        Config.WinePath = path;
                        Log.Info("Startup - XLCORE", Config.WinePath);
                        break;
                }
            }
            
            Config.WinePrefixPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".xlcore"), "wineprefix");
            SaveConfig();
            Process.Start(Config.XlCorePath);
        }
        
        Log.Info("Startup", "Starting RPCAPD");
        Process.Start(Config.RpcapdPath, "-l localhost -n"); // Listen on localhost only with no password auth.

        Log.Info("Startup", "Starting IINACT");
        ProcessStartInfo info = SetupWineProcessInfo(Config.IinactPath);
        info.EnvironmentVariables["DOTNET_BUNDLE_EXTRACT_BASE_DIR"] = Config.DotnetBundlePath; // dotnet6 apps require this env variable to be set. For some reason, WINE is not properly inheriting the default location when this env variable is not set.
        Process? iinact = Process.Start(info);
        
        iinact?.WaitForExit(); // Wait for IINACT to die, before killing rpcapd
        Process.Start("pkill", "-9 rpcapd");
        Log.Info("Program", "Good job, Kupo!");
    }

    private static void MakeFilepathsUsable()
    {
        if (Config.IinactPath.Contains("~/"))
            Config.IinactPath = Config.IinactPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (Config.RpcapdPath.Contains("~/"))
            Config.RpcapdPath = Config.RpcapdPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (Config.WinePath.Contains("~/"))
            Config.WinePath = Config.RpcapdPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (Config.DotnetBundlePath.Contains("~/"))
            Config.DotnetBundlePath = Config.DotnetBundlePath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (Config.XlCorePath.Contains("~/"))
            Config.XlCorePath = Config.XlCorePath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
        if (Config.WinePrefixPath.Contains("~/"))
            Config.WinePrefixPath = Config.WinePrefixPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }

    public static ProcessStartInfo SetupWineProcessInfo(string processName)
    {
        ProcessStartInfo startInfo = new();
        startInfo.EnvironmentVariables["WINEESYNC"] = Config.WineEsync ? "1" : "0";
        startInfo.EnvironmentVariables["WINEFSYNC"] = Config.WineFsync ? "1" : "0";
        startInfo.EnvironmentVariables["WINEPREFIX"] = Config.WinePrefixPath;
        if (Config.DisableWineLogs)
            startInfo.EnvironmentVariables["WINEDEBUG"] = "-all";
        if (Config.DisableDxvkLogs)
            startInfo.EnvironmentVariables["DXVK_LOG_LEVEL"] = "none";
        startInfo.Arguments = processName;
        startInfo.FileName = Config.WinePath;

        return startInfo;
    }

    private static void SaveConfig() => File.WriteAllText(Path.Combine(Assembly.GetExecutingAssembly().Location, KCfgFile), JsonConvert.SerializeObject(Config, Formatting.Indented));

    private static Config GetConfig()
    {
        if (File.Exists(Path.Combine(Assembly.GetExecutingAssembly().Location, KCfgFile)))
            return JsonConvert.DeserializeObject<Config>(File.ReadAllText(KCfgFile))!;
        File.WriteAllText(Path.Combine(Assembly.GetExecutingAssembly().Location, KCfgFile), JsonConvert.SerializeObject(Config.Default, Formatting.Indented));
        return Config.Default;
    }
}