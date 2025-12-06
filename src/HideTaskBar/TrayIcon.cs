using System.Windows.Forms;

namespace HideTaskBar;

/// <summary>
/// タスクトレイアイコンを管理する
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;

    public TrayIcon()
    {
        _contextMenu = new ContextMenuStrip();
        _contextMenu.Items.Add("終了(&X)", null, OnExitClick);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application, // 後でカスタムアイコンに変更可能
            Text = "HideTaskBar",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };
    }

    private void OnExitClick(object? sender, EventArgs e)
    {
        Application.Exit();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _contextMenu.Dispose();
    }
}
