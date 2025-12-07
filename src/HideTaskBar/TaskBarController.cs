using System.Runtime.InteropServices;

namespace HideTaskBar;

/// <summary>
/// タスクバーの表示/非表示を制御する
/// </summary>
public sealed class TaskBarController : IDisposable
{
    private const string TaskBarClassName = "Shell_TrayWnd";
    private const string SecondaryTaskBarClassName = "Shell_SecondaryTrayWnd";

    private const int SW_HIDE = 0;
    private const int SW_SHOW = 5;
    private const int GWL_EXSTYLE = -20;
    private const int WS_EX_TOOLWINDOW = 0x00000080;
    private const int WS_EX_APPWINDOW = 0x00040000;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    private readonly List<IntPtr> _taskBarHandles = [];
    private readonly Dictionary<IntPtr, int> _originalStyles = [];
    private readonly System.Windows.Forms.Timer _enforceTimer;
    private bool _shouldBeHidden = false;

    public TaskBarController()
    {
        RefreshTaskBarHandles();
        
        // 定期的に非表示状態を強制する
        _enforceTimer = new System.Windows.Forms.Timer
        {
            Interval = 200 // 200ms間隔
        };
        _enforceTimer.Tick += (s, e) =>
        {
            if (_shouldBeHidden)
            {
                EnforceHide();
            }
        };
        _enforceTimer.Start();
    }

    /// <summary>
    /// タスクバーのウィンドウハンドルを取得/更新する
    /// </summary>
    private void RefreshTaskBarHandles()
    {
        _taskBarHandles.Clear();

        // メインタスクバー
        var mainTaskBar = FindWindow(TaskBarClassName, null);
        if (mainTaskBar != IntPtr.Zero)
        {
            _taskBarHandles.Add(mainTaskBar);
        }

        // セカンダリモニターのタスクバー（マルチモニター対応の準備）
        IntPtr secondaryTaskBar = IntPtr.Zero;
        while ((secondaryTaskBar = FindWindowEx(IntPtr.Zero, secondaryTaskBar, SecondaryTaskBarClassName, null)) != IntPtr.Zero)
        {
            _taskBarHandles.Add(secondaryTaskBar);
        }
    }

    /// <summary>
    /// 非表示状態を強制する
    /// </summary>
    private void EnforceHide()
    {
        foreach (var handle in _taskBarHandles)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }

    /// <summary>
    /// タスクバーを非表示にする
    /// </summary>
    public void Hide()
    {
        _shouldBeHidden = true;
        foreach (var handle in _taskBarHandles)
        {
            ShowWindow(handle, SW_HIDE);
        }
    }

    /// <summary>
    /// タスクバーを表示する
    /// </summary>
    public void Show()
    {
        _shouldBeHidden = false;
        foreach (var handle in _taskBarHandles)
        {
            ShowWindow(handle, SW_SHOW);
        }
    }

    public void Dispose()
    {
        _enforceTimer.Stop();
        _enforceTimer.Dispose();
        
        // 終了時にタスクバーを復元
        _shouldBeHidden = false;
        Show();
    }
}
