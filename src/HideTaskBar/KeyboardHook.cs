using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HideTaskBar;

/// <summary>
/// グローバルキーボードフックでWinキー押下を検知する
/// </summary>
public sealed class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;
    private const int VK_LWIN = 0x5B;
    private const int VK_RWIN = 0x5C;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private readonly LowLevelKeyboardProc _proc;
    private readonly GCHandle _gcHandle; // GCによるデリゲート回収を防ぐ
    private IntPtr _hookId;
    private readonly System.Windows.Forms.Timer _rehookTimer;

    /// <summary>
    /// Winキーが押下された時に発火するイベント
    /// </summary>
    public event Action? WinKeyPressed;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _gcHandle = GCHandle.Alloc(_proc); // デリゲートを固定
        _hookId = SetHook(_proc);

        // 定期的にフックを再確認
        _rehookTimer = new System.Windows.Forms.Timer
        {
            Interval = 5000 // 5秒ごと
        };
        _rehookTimer.Tick += (s, e) => EnsureHook();
        _rehookTimer.Start();
    }

    private void EnsureHook()
    {
        if (_hookId == IntPtr.Zero)
        {
            _hookId = SetHook(_proc);
        }
    }

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        var handle = SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule?.ModuleName), 0);
        return handle;
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            
            // Winキーの押下を検知（KEYDOWNのみ）
            if ((wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN) &&
                (vkCode == VK_LWIN || vkCode == VK_RWIN))
            {
                WinKeyPressed?.Invoke();
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    public void Dispose()
    {
        _rehookTimer.Stop();
        _rehookTimer.Dispose();
        
        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }
        
        if (_gcHandle.IsAllocated)
        {
            _gcHandle.Free();
        }
    }
}
