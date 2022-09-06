namespace LaunchFfxiv;

public class Log
{
    public static void Info(string source, object msg) => Console.WriteLine($"[INFO] {source}: {msg}");

    public static void Debug(string source, object msg)
    {
        if (Program.Config.Debug)
            Console.WriteLine($"[DEBUG] {source}: {msg}");
    }

    public static void Error(string source, object msg) => Console.WriteLine($"[ERROR] {source}: {msg}");

    public static void Warn(string source, object msg) => Console.WriteLine($"[WARN] {source}: {msg}");
}