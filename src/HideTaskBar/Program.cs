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

        // 有効/無効切り替え時の処理（FR-5）
        trayIcon.EnabledChanged += (enabled) =>
        {
            if (enabled)
            {
                taskBarController.Hide();
            }
            else
            {
                taskBarController.Show();
            }
        };

        // Winキー押下時にタスクバーを表示
        keyboardHook.WinKeyPressed += () =>
        {
            if (trayIcon.IsEnabled)
            {
                taskBarController.Show();
                startMenuMonitor.StartWaitingForClose(); // 閉じるのを待機開始
            }
        };

        // スタートメニューが閉じたらタスクバーを非表示
        startMenuMonitor.StartMenuClosed += () =>
        {
            if (trayIcon.IsEnabled)
            {
                taskBarController.Hide();
            }
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