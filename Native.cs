using System;
using System.Runtime.InteropServices;

namespace SlideShot
{
    internal static class Native
    {
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_COLOR_NONE = unchecked((int)0xFFFFFFFE);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        public static extern int SetWindowTheme(IntPtr hwnd, string subApp, string subIdList);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool DeleteFileW(string fileName);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindow(IntPtr hwnd, uint cmd);

        private const uint GW_CHILD = 5;
        private const uint GW_HWNDNEXT = 2;

        public static void ApplyWindowChrome(IntPtr handle, bool dark)
        {
            if (handle == IntPtr.Zero) return;
            int preference = 2;
            DwmSetWindowAttribute(handle, DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
            int flag = dark ? 1 : 0;
            DwmSetWindowAttribute(handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref flag, sizeof(int));
            int border = DWMWA_COLOR_NONE;
            DwmSetWindowAttribute(handle, DWMWA_BORDER_COLOR, ref border, sizeof(int));
        }

        public static void ApplyDarkScrollbars(IntPtr handle)
        {
            if (handle == IntPtr.Zero) return;
            string mode = Theme.IsDark ? "DarkMode_Explorer" : "Explorer";
            SetWindowTheme(handle, mode, null);
            IntPtr child = GetWindow(handle, GW_CHILD);
            while (child != IntPtr.Zero)
            {
                SetWindowTheme(child, mode, null);
                IntPtr next = GetWindow(child, GW_HWNDNEXT);
                child = next;
            }
        }

        public static void RemoveZoneIdentifier(string path)
        {
            try
            {
                DeleteFileW(path + ":Zone.Identifier");
            }
            catch
            {
            }
        }
    }
}
