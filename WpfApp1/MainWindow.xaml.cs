using Esatto.Win32.Windows;
using Esatto.Win32.Wpf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
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
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;

            // Set the location to be "snapped" to the left
            const int SidebarWidth = 600;
            this.WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = desktop.Left;
            this.Top = desktop.Top;
            this.Width = SidebarWidth;
            this.Height = desktop.Height;

            // Once we are shown, resize the other window to match
            void OnActivated(object? o, EventArgs e)
            {
                // Only trigger once
                this.Activated -= OnActivated;

                var prevWp = previous.GetWindowPlacement();
                if (prevWp.ShowCmd == ShowWindowCommand.Restore)
                {
                    // TOCTOU?
                    return;
                }

                prevWp.NormalPosition = new Rect(
                    desktop.Left + SidebarWidth - previousShadow.Left,
                    desktop.Top,
                    desktop.Width - SidebarWidth + previousShadow.Left + previousShadow.Right,
                    desktop.Height + previousShadow.Top + previousShadow.Bottom
                );
                previous.SetWindowPlacement(prevWp);
            }
            this.Activated += OnActivated;
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
