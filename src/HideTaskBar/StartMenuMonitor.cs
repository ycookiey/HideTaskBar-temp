using System.Runtime.InteropServices;

namespace HideTaskBar;

/// <summary>
/// アクティブウィンドウの変更を監視し、スタートメニュー以外になったらイベント発火
/// </summary>
public sealed class StartMenuMonitor : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    [DllImport("user32.dll")]
    private static extern IntPtr SetWinEventHook(
        uint eventMin, uint eventMax,
        IntPtr hmodWinEventProc, WinEventDelegate lpfnWinEventProc,
        uint idProcess, uint idThread, uint dwFlags);

    [DllImport("user32.dll")]
    private static extern bool UnhookWinEvent(IntPtr hWinEventHook);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetClassName(IntPtr hWnd, System.Text.StringBuilder lpClassName, int nMaxCount);

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

    private readonly WinEventDelegate _winEventProc;
    private readonly IntPtr _hookHandle;
    private bool _waitingForClose = false;

    public event Action? StartMenuClosed;

    public StartMenuMonitor()
    {
        _winEventProc = WinEventCallback;
        _hookHandle = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _winEventProc, 0, 0, WINEVENT_OUTOFCONTEXT);
    }

    /// <summary>
    /// Winキーが押されたら呼ぶ。スタートメニューが閉じるのを待機開始
    /// </summary>
    public void StartWaitingForClose()
    {
        _waitingForClose = true;
        DebugLogger.Log("Waiting for active window change...");
    }

    private void WinEventCallback(
        IntPtr hWinEventHook, uint eventType, IntPtr hwnd,
        int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        if (!_waitingForClose) return;

        var foreground = GetForegroundWindow();
        var classNameBuilder = new System.Text.StringBuilder(256);
        GetClassName(foreground, classNameBuilder, classNameBuilder.Capacity);
        var className = classNameBuilder.ToString();

        DebugLogger.Log($"Active window changed: {className}");

        // スタートメニュー関連でなければ、閉じたとみなす
        if (!IsStartMenuRelated(className))
        {
            _waitingForClose = false;
            DebugLogger.LogEvent("StartMenuClosed (active window changed)");
            StartMenuClosed?.Invoke();
        }
    }

    private static bool IsStartMenuRelated(string className)
    {
        return className == "Windows.UI.Core.CoreWindow" ||
               className == "Shell_TrayWnd" ||
               className == "Shell_SecondaryTrayWnd" ||
               className == "XamlExplorerHostIslandWindow";
    }

    public void Dispose()
    {
        UnhookWinEvent(_hookHandle);
    }
}
