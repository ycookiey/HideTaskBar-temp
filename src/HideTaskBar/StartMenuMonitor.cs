using System.Runtime.InteropServices;

namespace HideTaskBar;

/// <summary>
/// スタートメニューの状態を監視し、閉じた時にイベントを発火する
/// </summary>
public sealed class StartMenuMonitor : IDisposable
{
    private const int SW_HIDE = 0;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    private readonly System.Windows.Forms.Timer _pollTimer;
    private bool _wasVisible = false;

    /// <summary>
    /// スタートメニューが閉じられた時に発火するイベント
    /// </summary>
    public event Action? StartMenuClosed;

    public StartMenuMonitor()
    {
        _pollTimer = new System.Windows.Forms.Timer
        {
            Interval = 100 // 100ms間隔でポーリング
        };
        _pollTimer.Tick += OnTimerTick;
        _pollTimer.Start();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        bool isVisible = IsStartMenuVisible();

        if (_wasVisible && !isVisible)
        {
            // スタートメニューが閉じられた
            StartMenuClosed?.Invoke();
        }

        _wasVisible = isVisible;
    }

    /// <summary>
    /// スタートメニューが表示されているかどうかを直接確認
    /// </summary>
    private static bool IsStartMenuVisible()
    {
        // Windows 11 スタートメニューのウィンドウを検索
        // クラス名: Windows.UI.Core.CoreWindow, ウィンドウ名: スタート または Start
        IntPtr hwnd = FindStartMenuWindow();
        if (hwnd == IntPtr.Zero) return false;

        return IsWindowVisible(hwnd);
    }

    private static IntPtr FindStartMenuWindow()
    {
        // 日本語環境
        IntPtr hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Windows.UI.Core.CoreWindow", "スタート");
        if (hwnd != IntPtr.Zero) return hwnd;

        // 英語環境
        hwnd = FindWindowEx(IntPtr.Zero, IntPtr.Zero, "Windows.UI.Core.CoreWindow", "Start");
        return hwnd;
    }

    public void Dispose()
    {
        _pollTimer.Stop();
        _pollTimer.Dispose();
    }
}
