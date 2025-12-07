using Microsoft.Win32;

namespace HideTaskBar;

/// <summary>
/// Windowsスタートアップへの自動起動登録を管理する
/// </summary>
public static class AutoStartManager
{
    private const string AppName = "HideTaskBar";
    private const string RegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// スタートアップに登録されているかどうか
    /// </summary>
    public static bool IsRegistered()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, false);
        return key?.GetValue(AppName) != null;
    }

    /// <summary>
    /// スタートアップに登録する
    /// </summary>
    public static void Register()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
        var exePath = Environment.ProcessPath;
        if (key != null && exePath != null)
        {
            key.SetValue(AppName, $"\"{exePath}\"");
        }
    }

    /// <summary>
    /// スタートアップから解除する
    /// </summary>
    public static void Unregister()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RegistryKeyPath, true);
        key?.DeleteValue(AppName, false);
    }
}
