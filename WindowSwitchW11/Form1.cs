using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace WindowSwitchW11
{
    public partial class Form1 : Form
    {
        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_SHOWWINDOW = 0x0040;

        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool BringWindowToTop(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool IsIconic(IntPtr hWnd);

        private const uint SW_SHOW = 5;
        private const uint SW_RESTORE = 9;

        KeyboardHook hook;
        private List<WindowLabel> windowsOnForm = new List<WindowLabel>();
        private List<WindowInfo> lastWindowInfo;

        public Form1()
        {
            InitializeComponent();
            TopMost = true;
            ShowInTaskbar = false;
            hook = new KeyboardHook();
            hook.AltTabPressed += OnAltTabPressed;
            hook.EscapePressed += OnEscapePressed;
            hook.AltReleased += OnAltReleased;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams cp = base.CreateParams;
                cp.ExStyle |= 0x08000000; // WS_EX_NOACTIVATE (0x8000000 in hex, but correct is 0x08000000)
                return cp;
            }
        }

        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        private void OnAltTabPressed(object sender, KeyboardHook.AltTabPressedEventArgs args)
        {
            if (!Visible)
            {
                SetupWindowList();

                base.SetVisibleCore(true);

                // Force topmost and show
                SetWindowPos(this.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW);

                // Activate and bring to foreground
                ForceForegroundWindow(this.Handle);
            }
            else
            {
                bool anySelected = false;
                for (int i = 0; i < windowsOnForm.Count(); i++)
                {
                    if (windowsOnForm[i].Selected)
                    {
                        windowsOnForm[i].Selected = false;
                        if (args.ShiftPressed)
                        {
                            int next = i - 1;
                            if (next < 0)
                                next = windowsOnForm.Count - 1;
                            windowsOnForm[next].Selected = true;
                            label1.Text = lastWindowInfo[next].Title;
                        }
                        else
                        {
                            int next = (i + 1) % windowsOnForm.Count;
                            windowsOnForm[next].Selected = true;
                            label1.Text = lastWindowInfo[next].Title;
                        }
                        anySelected = true;
                        break;
                    }
                }
                if (!anySelected && windowsOnForm.Count > 0)
                {
                    windowsOnForm[0].Selected = true;
                }
            }
        }

        private void ForceForegroundWindow(IntPtr hWnd)
        {
            uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
            uint appThread = GetCurrentThreadId();

            if (foreThread != appThread)
            {
                AttachThreadInput(foreThread, appThread, true);
                BringWindowToTop(hWnd);
                SetForegroundWindow(hWnd);
                ShowWindow(hWnd, IsIconic(hWnd) ? SW_RESTORE : SW_SHOW);
                AttachThreadInput(foreThread, appThread, false);
            }
            else
            {
                BringWindowToTop(hWnd);
                SetForegroundWindow(hWnd);
                ShowWindow(hWnd, IsIconic(hWnd) ? SW_RESTORE : SW_SHOW);
            }
        }

        private void OnEscapePressed(object sender, EventArgs args)
        {
            // restore control to the window #1, which is always the currently focused window
            ForceForegroundWindow(lastWindowInfo[0].Handle);
            base.SetVisibleCore(false);
        }

        private void OnAltReleased(object sender, EventArgs args)
        {
            for (int i = 0; i < windowsOnForm.Count; i++)
            {
                if (windowsOnForm[i].Selected)
                {
                    IntPtr hWnd = lastWindowInfo[i].Handle;
                    ForceForegroundWindow(hWnd);
                }
            }

            base.SetVisibleCore(false);
        }

        private void SetupWindowList()
        {
            // for now, do very simple approach
            lastWindowInfo = WindowEnumerator.GetOpenWindows();
            foreach (var control in windowsOnForm)
                control.Dispose();
            windowsOnForm.Clear();
            int iconSize = 32;
            int maxIconsOnRow = 8;
            int padding = 8;
            int itemSize = iconSize + padding * 2;
            int y = 0;
            int x = 0;
            int remainderIcons = lastWindowInfo.Count % maxIconsOnRow;
            int lastLineStart = lastWindowInfo.Count - remainderIcons;
            int maxRows = lastWindowInfo.Count / maxIconsOnRow;
            if (remainderIcons != 0)
                maxRows++;
            int initialSelectedWindow = lastWindowInfo.Count > 1 ? 1 : 0;
            for (int i = 0; i < lastWindowInfo.Count; i++)
            {
                WindowInfo window = lastWindowInfo[i];
                if (window.Handle == Handle)
                    continue;
                int lineOffset = 0;
                if (i >= lastLineStart)
                {
                    lineOffset = (maxIconsOnRow * itemSize) / 2 - (remainderIcons * itemSize) / 2;
                }
                WindowLabel windowVisual = new WindowLabel();
                Label icon = new Label();

                // Scale the icon if smaller
                if (window.Icon != null)
                {
                    using (Bitmap original = window.Icon.ToBitmap())
                    {
                        Bitmap imageToUse;
                        if (original.Width < iconSize || original.Height < iconSize)
                        {
                            imageToUse = new Bitmap(iconSize, iconSize);
                            using (Graphics g = Graphics.FromImage(imageToUse))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                g.DrawImage(original, 0, 0, iconSize, iconSize);
                            }
                        }
                        else
                        {
                            imageToUse = (Bitmap)original.Clone();  // Clone to avoid disposing the original prematurely
                        }
                        icon.Image = imageToUse;
                    }
                }

                icon.Location = new Point(padding, padding);
                icon.Size = new Size(iconSize, iconSize);
                windowVisual.Location = new Point(lineOffset + x * itemSize, y * itemSize);
                windowVisual.Size = new Size(itemSize, itemSize);
                windowVisual.Selected = i == initialSelectedWindow;
                windowVisual.Controls.Add(icon);
                x += 1;
                if (x >= maxIconsOnRow)
                {
                    x = 0;
                    y++;
                }
                panel1.Controls.Add(windowVisual);
                windowsOnForm.Add(windowVisual);
            }
            // position and resize
            Size = new Size(padding * 2 + maxIconsOnRow * itemSize + 8, padding * 2 + maxRows * itemSize + 48);

            Point mousePos = Cursor.Position;
            Screen cursorScreen = Screen.FromPoint(mousePos);

            int centerX = cursorScreen.Bounds.Left + (cursorScreen.Bounds.Width - this.Width) / 2;
            int centerY = cursorScreen.Bounds.Top + (cursorScreen.Bounds.Height - this.Height) / 2;
            this.Location = new Point(centerX, centerY);

            label1.Text = lastWindowInfo[initialSelectedWindow].Title;
        }
    }
}