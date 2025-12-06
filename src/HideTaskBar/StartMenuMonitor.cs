using System.Runtime.InteropServices;

namespace HideTaskBar;

/// <summary>
/// スタートメニューの状態を監視し、閉じた時にイベントを発火する
/// </summary>
public sealed class StartMenuMonitor : IDisposable
{
    private const uint EVENT_SYSTEM_FOREGROUND = 0x0003;
    private const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    private const string StartMenuClassName = "Windows.UI.Core.CoreWindow";
    private const string StartMenuWindowName = "スタート"; // Windows 11 日本語

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

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr hWnd, System.Text.StringBuilder lpString, int nMaxCount);

    private delegate void WinEventDelegate(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime);

    private readonly WinEventDelegate _winEventProc;
    private readonly IntPtr _hookHandle;
    private bool _startMenuWasOpen = false;

    /// <summary>
    /// スタートメニューが閉じられた時に発火するイベント
    /// </summary>
    public event Action? StartMenuClosed;

    public StartMenuMonitor()
    {
        _winEventProc = WinEventCallback;
        _hookHandle = SetWinEventHook(
            EVENT_SYSTEM_FOREGROUND, EVENT_SYSTEM_FOREGROUND,
            IntPtr.Zero, _winEventProc,
            0, 0, WINEVENT_OUTOFCONTEXT);
    }

    private void WinEventCallback(
        IntPtr hWinEventHook, uint eventType,
        IntPtr hwnd, int idObject, int idChild,
        uint dwEventThread, uint dwmsEventTime)
    {
        bool isStartMenuNowOpen = IsStartMenuForeground();

        if (_startMenuWasOpen && !isStartMenuNowOpen)
        {
            // スタートメニューが閉じられた
            StartMenuClosed?.Invoke();
        }

        _startMenuWasOpen = isStartMenuNowOpen;
    }

    private static bool IsStartMenuForeground()
    {
        var foreground = GetForegroundWindow();
        if (foreground == IntPtr.Zero) return false;

        var classNameBuilder = new System.Text.StringBuilder(256);
        GetClassName(foreground, classNameBuilder, classNameBuilder.Capacity);
        var className = classNameBuilder.ToString();

        // Windows 11のスタートメニューはCoreWindowクラス
        if (className != StartMenuClassName) return false;

        var windowTextBuilder = new System.Text.StringBuilder(256);
        GetWindowText(foreground, windowTextBuilder, windowTextBuilder.Capacity);
        var windowText = windowTextBuilder.ToString();

        // 日本語・英語両対応
        return windowText == "スタート" || windowText == "Start";
    }

    public void Dispose()
    {
        UnhookWinEvent(_hookHandle);
    }
}
