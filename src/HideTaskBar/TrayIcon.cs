using System.Windows.Forms;

namespace HideTaskBar;

/// <summary>
/// タスクトレイアイコンを管理する
/// </summary>
public sealed class TrayIcon : IDisposable
{
    private readonly NotifyIcon _notifyIcon;
    private readonly ContextMenuStrip _contextMenu;
    private readonly ToolStripMenuItem _enabledMenuItem;
    private readonly ToolStripMenuItem _autoStartMenuItem;

    private bool _isEnabled = true;

    /// <summary>
    /// 有効/無効状態が変更された時に発火するイベント
    /// </summary>
    public event Action<bool>? EnabledChanged;

    /// <summary>
    /// 現在有効かどうか
    /// </summary>
    public bool IsEnabled => _isEnabled;

    public TrayIcon()
    {
        _contextMenu = new ContextMenuStrip();

        // 有効/無効メニュー（FR-5）
        _enabledMenuItem = new ToolStripMenuItem("有効(&E)")
        {
            Checked = true,
            CheckOnClick = true
        };
        _enabledMenuItem.CheckedChanged += OnEnabledChanged;
        _contextMenu.Items.Add(_enabledMenuItem);

        // スタートアップ登録メニュー（FR-6）
        _autoStartMenuItem = new ToolStripMenuItem("スタートアップ登録(&S)")
        {
            Checked = AutoStartManager.IsRegistered(),
            CheckOnClick = true
        };
        _autoStartMenuItem.CheckedChanged += OnAutoStartChanged;
        _contextMenu.Items.Add(_autoStartMenuItem);

        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add("終了(&X)", null, OnExitClick);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = "HideTaskBar",
            ContextMenuStrip = _contextMenu,
            Visible = true
        };
    }

    private void OnEnabledChanged(object? sender, EventArgs e)
    {
        _isEnabled = _enabledMenuItem.Checked;
        EnabledChanged?.Invoke(_isEnabled);
    }

    private void OnAutoStartChanged(object? sender, EventArgs e)
    {
        if (_autoStartMenuItem.Checked)
        {
            AutoStartManager.Register();
        }
        else
        {
            AutoStartManager.Unregister();
        }
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
