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

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private readonly List<IntPtr> _taskBarHandles = [];

    public TaskBarController()
    {
        RefreshTaskBarHandles();
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
    /// タスクバーを非表示にする
    /// </summary>
    public void Hide()
    {
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
        foreach (var handle in _taskBarHandles)
        {
            ShowWindow(handle, SW_SHOW);
        }
    }

    public void Dispose()
    {
        // 終了時にタスクバーを復元
        Show();
    }
}
