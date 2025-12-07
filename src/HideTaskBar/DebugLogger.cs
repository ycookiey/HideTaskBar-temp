namespace HideTaskBar;

/// <summary>
/// デバッグ用ログ出力
/// </summary>
public static class DebugLogger
{
    private static readonly string LogPath = Path.Combine(
        AppContext.BaseDirectory, 
        $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log"
    );

    private static readonly object _lock = new();

    public static void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = $"[{timestamp}] {message}";

        lock (_lock)
        {
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
    }

    public static void LogStartMenuState(bool isVisible)
    {
        Log($"StartMenu visible: {isVisible}");
    }

    public static void LogWinKeyPressed()
    {
        Log("WinKey pressed");
    }

    public static void LogTaskBarAction(string action)
    {
        Log($"TaskBar: {action}");
    }

    public static void LogEvent(string eventName)
    {
        Log($"Event: {eventName}");
    }
}
