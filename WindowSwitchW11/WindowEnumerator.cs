using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;

public class WindowInfo
{
    public IntPtr Handle { get; set; }
    public string Title { get; set; }
    public Icon Icon { get; set; }
}

public static class WindowEnumerator
{
    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("dwmapi.dll")]
    private static extern int DwmGetWindowAttribute(IntPtr hWnd, int dwAttribute, out int pvAttribute, int cbAttribute);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

    private const uint WM_GETICON = 0x007F;
    private const int ICON_SMALL = 0;
    private const int ICON_BIG = 1;
    private const int DWMWA_CLOAKED = 14;

    public static List<WindowInfo> GetOpenWindows()
    {
        List<WindowInfo> windows = new List<WindowInfo>();
        uint currentProcessId = (uint)Process.GetCurrentProcess().Id;
        EnumWindows(delegate (IntPtr hWnd, IntPtr param)
        {
            if (IsWindowVisible(hWnd))
            {
                // Check if cloaked (hidden from user view, e.g., background UWP windows)
                int cloaked = 0;
                DwmGetWindowAttribute(hWnd, DWMWA_CLOAKED, out cloaked, sizeof(int));
                if (cloaked != 0)
                    return true; // Skip cloaked windows
                StringBuilder className = new StringBuilder(256);
                GetClassName(hWnd, className, className.Capacity);
                if (className.ToString() == "Progman")
                    return true; // Skip Program Manager (desktop) window
                GetWindowThreadProcessId(hWnd, out uint pid);
                if (pid == currentProcessId)
                    return true; // Skip windows from the current process
                StringBuilder title = new StringBuilder(256);
                GetWindowText(hWnd, title, title.Capacity);
                if (title.Length == 0)
                    return true;
                // Get icon
                Icon icon = null;
                IntPtr iconHandle = SendMessage(hWnd, WM_GETICON, ICON_BIG, 0);
                if (iconHandle == IntPtr.Zero)
                {
                    // Fallback: Get from process executable
                    Process proc = Process.GetProcessById((int)pid);
                    if (proc.MainModule?.FileName != null && !string.IsNullOrEmpty(proc.MainModule.FileName))
                    {
                        icon = Icon.ExtractAssociatedIcon(proc.MainModule.FileName);
                    }
                }
                else
                {
                    icon = Icon.FromHandle(iconHandle);
                }

                windows.Add(new WindowInfo { Handle = hWnd, Title = title.ToString(), Icon = icon });
            }
            return true;
        }, IntPtr.Zero);
        return windows;
    }
}