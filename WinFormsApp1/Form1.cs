using Esatto.Win32.Windows;
using Esatto.Win32.Wpf;
using Windows.Win32;

namespace WinFormsApp1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            // Record the previous foreground window
            var previous = GetPreviousWindow();

            // Ensure it is not maximized, lest we resize a maximized window
            // (Maximized windows do not have a shadow, so their dimensions are funny.
            // They also tend to freak out when they are resized)
            if (previous.GetIsMaximized())
            {
                previous.Restore();
            }

            // Find the monitor which contains the center of the previous window
            var (previousRect, previousShadow) = previous.GetBoundsWithShadowThickness();
            var previousCenter = previousRect.GetCenter();
            var desktop = MonitorInfo.GetAllMonitors()
                .Single(f => f.ViewportBounds.Contains(previousCenter))
                .WorkAreaBounds;

            // Normal form border includes large resize border - which is visually not part of the window.
            this.FormBorderStyle = FormBorderStyle.None;

            // Set the location to be "snapped" to the left
            this.StartPosition = FormStartPosition.Manual;
            var ourPos = new Rectangle((int)desktop.Left, (int)desktop.Top, 600, (int)desktop.Height);
            this.DesktopBounds = ourPos;

            // Once we are shown, resize the other window to match
            this.Shown += (_, _) =>
            {
                var prevWp = previous.GetWindowPlacement();
                if (prevWp.ShowCmd == ShowWindowCommand.Restore)
                {
                    // TOCTOU?
                    return;
                }

                prevWp.NormalPosition = new System.Windows.Rect(
                    ourPos.Right - previousShadow.Left,
                    ourPos.Top,
                    desktop.Width - ourPos.Width + previousShadow.Left + previousShadow.Right,
                    desktop.Height + previousShadow.Top + previousShadow.Bottom
                );
                previous.SetWindowPlacement(prevWp);
            };
        }

        private static Win32Window GetPreviousWindow()
        {
            var fg = Win32Window.GetForegroundWindow();

            if (IsAcceptableWindow(fg))
            {
                return fg;
            }

            throw new InvalidOperationException("Foreground window is not acceptable");
        }

        private static bool IsAcceptableWindow(Win32Window fg)
        {
            if (fg.GetParent().Handle != IntPtr.Zero)
            {
                // not a top-level window
                return false;
            }

            var style = fg.GetWindowStyle();
            if (style.HasFlag(WindowStyles.Child))
            {
                return false;
            }

            if (!style.HasFlag(WindowStyles.SizeFrame))
            {
                // Non-resizable
                return false;
            }

            if (!fg.GetIsShown())
            {
                return false;
            }

            return true;
        }
    }
}