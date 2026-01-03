using System;
using System.Runtime.InteropServices;

namespace WinSpotlight
{
    internal static class DwmApi
    {
        private const int DWMWA_SYSTEMBACKDROP_TYPE = 38;
        private const int DWMWA_MICA_EFFECT = 1029;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        public static void EnableMica(IntPtr hwnd)
        {
            int trueValue = 1;
            DwmSetWindowAttribute(hwnd, DWMWA_MICA_EFFECT, ref trueValue, sizeof(int));
            int mica = 2; // DWMSBT_TRANSIENT
            DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref mica, sizeof(int));
        }
    }
}
