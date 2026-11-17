using Microsoft.Win32;
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;

namespace Fluent11Helper
{
    internal static class MicaBackdrop
    {
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMSBT_AUTO = 0;
        private const int DWMSBT_MAINWINDOW = 2;
        private const int DWMSBT_TRANSIENTWINDOW = 3;

        private static readonly DependencyProperty OriginalColorProperty =
            DependencyProperty.RegisterAttached("OriginalColor", typeof(Color?), typeof(MicaBackdrop), new PropertyMetadata(null));

        [StructLayout(LayoutKind.Sequential)]
        private struct Margins
        {
            public int Left, Right, Top, Bottom;
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("dwmapi.dll")]
        private static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        private const int GWL_STYLE = -16;
        private const int WS_SYSMENU = 0x00080000;
        private const uint SWP_FRAMECHANGED = 0x0020, SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001, SWP_NOZORDER = 0x0004, SWP_NOACTIVATE = 0x0010;

        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int cx, int cy, uint flags);

        private static void SetSystemMenu(IntPtr hwnd, bool visible)
        {
            var style = GetWindowLong(hwnd, GWL_STYLE);
            var newStyle = visible ? style | WS_SYSMENU : style & ~WS_SYSMENU;
            if (newStyle != style)
            {
                SetWindowLong(hwnd, GWL_STYLE, newStyle);
                SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_FRAMECHANGED | SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
            }
        }

        public static bool IsSupported()
        {
            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
                {
                    return int.TryParse(key?.GetValue("CurrentBuildNumber") as string, out var build) && build >= 22621;
                }
            }
            catch
            {
                return false;
            }
        }

        public static bool Enable(Window window, Brush tint, bool acrylic = false)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || window.AllowsTransparency)
            {
                return false;
            }

            var backdrop = acrylic ? DWMSBT_TRANSIENTWINDOW : DWMSBT_MAINWINDOW;
            if (window.GetValue(OriginalColorProperty) != null)
            {
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));
                window.Background = tint;
                return true;
            }

            var source = HwndSource.FromHwnd(hwnd);
            if (source?.CompositionTarget == null)
            {
                return false;
            }

            if (DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int)) != 0)
            {
                return false;
            }

            var chrome = WindowChrome.GetWindowChrome(window);
            if (chrome != null)
            {
                var clone = (WindowChrome)chrome.Clone();
                clone.GlassFrameThickness = new Thickness(-1);
                WindowChrome.SetWindowChrome(window, clone);
                SetSystemMenu(hwnd, false);
            }
            else
            {
                var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(hwnd, ref margins);
            }

            window.SetValue(OriginalColorProperty, (Color?)source.CompositionTarget.BackgroundColor);
            source.CompositionTarget.BackgroundColor = Colors.Transparent;
            window.Background = tint;
            return true;
        }

        public static void Disable(Window window)
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero || !(window.GetValue(OriginalColorProperty) is Color original))
            {
                return;
            }

            var backdrop = DWMSBT_AUTO;
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref backdrop, sizeof(int));

            if (window.ReadLocalValue(WindowChrome.WindowChromeProperty) != DependencyProperty.UnsetValue)
            {
                SetSystemMenu(hwnd, true);
                window.ClearValue(WindowChrome.WindowChromeProperty);
            }
            else
            {
                var margins = new Margins();
                DwmExtendFrameIntoClientArea(hwnd, ref margins);
            }

            window.ClearValue(Window.BackgroundProperty);
            var source = HwndSource.FromHwnd(hwnd);
            if (source?.CompositionTarget != null)
            {
                source.CompositionTarget.BackgroundColor = original;
            }

            window.ClearValue(OriginalColorProperty);
        }
    }
}
