using System.Windows.Forms;

namespace HideTaskBar;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        using var trayIcon = new TrayIcon();
        using var taskBarController = new TaskBarController();
        using var keyboardHook = new KeyboardHook();
        using var startMenuMonitor = new StartMenuMonitor();

        // Winキー押下時にタスクバーを表示
        keyboardHook.WinKeyPressed += () =>
        {
            taskBarController.Show();
        };

        // スタートメニューが閉じたらタスクバーを非表示
        startMenuMonitor.StartMenuClosed += () =>
        {
            taskBarController.Hide();
        };

        // 初期状態でタスクバーを非表示
        taskBarController.Hide();

        // アプリケーション終了時にタスクバーを復元
        Application.ApplicationExit += (s, e) =>
        {
            taskBarController.Show();
        };

        Application.Run();
    }
}