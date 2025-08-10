using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WindowSwitchW11
{
    public partial class Form1 : Form
    {
        const int MAX_ROWS = 3;
        const int MAX_COLUMNS = 7;
        const int ICON_SIZE = 32;

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
        private List<Image?> images = new List<Image?>();
        private int scroll = 0;

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
                if (!SetupWindowList())
                {
                    hook.CancelHook();
                    return;
                }

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
                            int next = scroll + i - 1;
                            if (next < 0)
                                next = lastWindowInfo.Count - 1;
                            MakeVisibleAndSelect(next);
                            label1.Text = lastWindowInfo[next].Title;
                        }
                        else
                        {
                            int next = (scroll + i + 1) % lastWindowInfo.Count;
                            MakeVisibleAndSelect(next);
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
                    IntPtr hWnd = lastWindowInfo[scroll+i].Handle;
                    ForceForegroundWindow(hWnd);
                }
            }

            base.SetVisibleCore(false);
        }

        private bool SetupWindowList()
        {
            if (lastWindowInfo != null)
            {
                foreach (var info in lastWindowInfo)
                {
                    info.Icon?.Dispose();
                }
            }
            // for now, do very simple approach
            lastWindowInfo = WindowEnumerator.GetOpenWindows();
            foreach (var image in images)
                image.Dispose();
            images.Clear();
            foreach (var control in windowsOnForm)
                control.Dispose();
            windowsOnForm.Clear();
            if (lastWindowInfo.Count == 0)
                return false;
            foreach (var window in lastWindowInfo)
            {
                Image? image = null;
                // Scale the icon if smaller
                if (window.Icon != null)
                {
                    using (Bitmap original = window.Icon.ToBitmap())
                    {
                        Bitmap imageToUse;
                        if (original.Width != ICON_SIZE || original.Height != ICON_SIZE)
                        {
                            imageToUse = new Bitmap(ICON_SIZE, ICON_SIZE);
                            using (Graphics g = Graphics.FromImage(imageToUse))
                            {
                                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
                                g.DrawImage(original, 0, 0, ICON_SIZE, ICON_SIZE);
                            }
                        }
                        else
                        {
                            imageToUse = (Bitmap)original.Clone();  // Clone to avoid disposing the original prematurely
                        }
                        image = imageToUse;
                    }
                }
                images.Add(image);
            }
            scroll = 0;
            int initialSelectedWindow = lastWindowInfo.Count > 1 ? 1 : 0;
            SetupWindowSubList(initialSelectedWindow);
            return true;
        }

        private void MakeVisibleAndSelect(int next)
        {
            // which row is scroll?
            int currentRow = scroll / MAX_COLUMNS;
            // which row "next" is in?
            int nextRow = next / MAX_COLUMNS;
            if (nextRow < currentRow)
            {
                scroll = nextRow * MAX_COLUMNS;
            }
            else if (nextRow >= currentRow + MAX_ROWS)
            {
                scroll = (nextRow - (MAX_ROWS-1)) * MAX_COLUMNS;
            }
            else
            {
                windowsOnForm[next - scroll].Selected = true;
                return;
            }
            SetupWindowSubList(next);
        }

        private void SetupWindowSubList(int initialSelectedWindow)
        {
            foreach (var control in windowsOnForm)
                control.Dispose();
            windowsOnForm.Clear();
            int padding = 6;
            int itemSize = ICON_SIZE + padding * 2;
            int y = 0;
            int x = 0;
            int displayCount = lastWindowInfo.Count - scroll;
            int remainderIcons = (displayCount) % MAX_COLUMNS;
            int lastLineStart = scroll + (displayCount - remainderIcons);
            int maxRows = displayCount / MAX_COLUMNS;
            if (remainderIcons != 0)
                maxRows++;
            if (maxRows > MAX_ROWS)
                maxRows = MAX_ROWS;
            for (int i = scroll; i < lastWindowInfo.Count; i++)
            {
                WindowInfo window = lastWindowInfo[i];
                if (window.Handle == Handle)
                    continue;
                int lineOffset = 0;
                if (i >= lastLineStart)
                {
                    lineOffset = (MAX_COLUMNS * itemSize) / 2 - (remainderIcons * itemSize) / 2;
                }
                WindowLabel windowVisual = new WindowLabel();
                Label icon = new Label();
                icon.Image = images[i];
                icon.Location = new Point(padding, padding);
                icon.Size = new Size(ICON_SIZE, ICON_SIZE);
                windowVisual.Location = new Point(lineOffset + x * itemSize, y * itemSize);
                windowVisual.Size = new Size(itemSize, itemSize);
                windowVisual.Selected = i == initialSelectedWindow;
                windowVisual.Controls.Add(icon);
                panel1.Controls.Add(windowVisual);
                windowsOnForm.Add(windowVisual);
                x += 1;
                if (x >= MAX_COLUMNS)
                {
                    x = 0;
                    y++;
                    if (y >= maxRows)
                        break;
                }
            }
            // position and resize
            Size = new Size(padding * 2 + MAX_COLUMNS * itemSize + 12, padding * 2 + maxRows * itemSize + 54);

            Point mousePos = Cursor.Position;
            Screen cursorScreen = Screen.FromPoint(mousePos);

            int centerX = cursorScreen.Bounds.Left + (cursorScreen.Bounds.Width - this.Width) / 2;
            int centerY = cursorScreen.Bounds.Top + (cursorScreen.Bounds.Height - this.Height) / 2;
            this.Location = new Point(centerX, centerY);

            label1.Text = lastWindowInfo[initialSelectedWindow].Title;
        }
    }
}