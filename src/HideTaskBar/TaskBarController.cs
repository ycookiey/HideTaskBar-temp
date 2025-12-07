using System.Runtime.InteropServices;

namespace HideTaskBar;

/// <summary>
/// タスクバーの表示/非表示を制御する（複合方式: SetWindowPos + ShowWindow）
/// </summary>
public sealed class TaskBarController : IDisposable
{
    private const string TaskBarClassName = "Shell_TrayWnd";
    private const string SecondaryTaskBarClassName = "Shell_SecondaryTrayWnd";

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const int HIDDEN_Y_OFFSET = -10000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left, Top, Right, Bottom;
    }

    private readonly List<IntPtr> _taskBarHandles = [];
    private readonly Dictionary<IntPtr, RECT> _originalPositions = [];
    private readonly System.Windows.Forms.Timer _enforceTimer;
    private bool _shouldBeHidden = false;

    public TaskBarController()
    {
        RefreshTaskBarHandles();
        SaveOriginalPositions();
        
        // 50ms間隔で非表示状態を強制（より頻繁に）
        _enforceTimer = new System.Windows.Forms.Timer { Interval = 50 };
        _enforceTimer.Tick += (s, e) =>
        {
            if (_shouldBeHidden)
            {
                EnforceHide();
            }
        };
        _enforceTimer.Start();
    }

    private void RefreshTaskBarHandles()
    {
        _taskBarHandles.Clear();
        var main = FindWindow(TaskBarClassName, null);
        if (main != IntPtr.Zero) _taskBarHandles.Add(main);

        IntPtr secondary = IntPtr.Zero;
        while ((secondary = FindWindowEx(IntPtr.Zero, secondary, SecondaryTaskBarClassName, null)) != IntPtr.Zero)
        {
            _taskBarHandles.Add(secondary);
        }
    }

    private void SaveOriginalPositions()
    {
        foreach (var handle in _taskBarHandles)
        {
            if (GetWindowRect(handle, out RECT rect))
            {
                _originalPositions[handle] = rect;
                DebugLogger.Log($"Original position: Left={rect.Left}, Top={rect.Top}");
            }
        }
    }

    private void EnforceHide()
    {
        foreach (var handle in _taskBarHandles)
        {
            // ShowWindowとSetWindowPosの両方を使用
            ShowWindow(handle, SW_HIDE);
            
            if (_originalPositions.TryGetValue(handle, out RECT original))
            {
                SetWindowPos(handle, IntPtr.Zero, original.Left, HIDDEN_Y_OFFSET, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }
    }

    public void Hide()
    {
        DebugLogger.LogTaskBarAction("Hide");
        _shouldBeHidden = true;
        EnforceHide();
    }

    public void Show()
    {
        DebugLogger.LogTaskBarAction("Show");
        _shouldBeHidden = false;
        
        foreach (var handle in _taskBarHandles)
        {
            // 元の位置に戻してから表示
            if (_originalPositions.TryGetValue(handle, out RECT original))
            {
                SetWindowPos(handle, IntPtr.Zero, original.Left, original.Top, 0, 0,
                    SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
            ShowWindow(handle, SW_SHOW);
        }
    }

    public void Dispose()
    {
        _enforceTimer.Stop();
        _enforceTimer.Dispose();
        _shouldBeHidden = false;
        Show();
    }
}
