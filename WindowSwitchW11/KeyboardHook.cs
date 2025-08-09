using System;
using System.Diagnostics;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Windows.Forms;

public class KeyboardHook : IDisposable
{
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    private LowLevelKeyboardProc _proc;
    private IntPtr _hookID = IntPtr.Zero;

    public class AltTabPressedEventArgs : EventArgs
    {
        public bool ShiftPressed { get; set; }
    }

    public event EventHandler<AltTabPressedEventArgs> AltTabPressed;
    public event EventHandler AltReleased;

    private bool _hookEnabled = false;

    public KeyboardHook()
    {
        _proc = HookCallback;
        _hookID = SetHook(_proc);
    }

    public void Dispose()
    {
        UnhookWindowsHookEx(_hookID);
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    private IntPtr SetHook(LowLevelKeyboardProc proc)
    {
        using (Process curProcess = Process.GetCurrentProcess())
        using (ProcessModule curModule = curProcess.MainModule)
        {
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            Keys key = (Keys)vkCode;

            if (wParam == (IntPtr)WM_KEYDOWN || wParam == (IntPtr)WM_SYSKEYDOWN)
            {
                bool altDown = (Control.ModifierKeys & Keys.Alt) != 0;
                if (altDown && key == Keys.Tab)
                {
                    var args = new AltTabPressedEventArgs
                    {
                        ShiftPressed = (Control.ModifierKeys & Keys.Shift) != 0
                    };
                    AltTabPressed?.Invoke(this, args);
                    _hookEnabled = true;
                    return (IntPtr)1; // Consume the message to prevent system handling
                }
            }
            else if (wParam == (IntPtr)WM_KEYUP || wParam == (IntPtr)WM_SYSKEYUP)
            {
                if (_hookEnabled && (key == Keys.Menu || key == Keys.LMenu || key == Keys.RMenu || vkCode == 0x12)) // Alt keys (VK_MENU = 0x12)
                {
                    AltReleased?.Invoke(this, EventArgs.Empty);
                    _hookEnabled = false;
                    // NEVER consume the alt release, because we also don't consume alt press.
                    return CallNextHookEx(_hookID, nCode, wParam, lParam);
                }
            }
        }
        return CallNextHookEx(_hookID, nCode, wParam, lParam);
    }

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);
}