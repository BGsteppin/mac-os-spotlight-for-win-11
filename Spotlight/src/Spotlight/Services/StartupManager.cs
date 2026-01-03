using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace WinSpotlight.Services
{
    public static class StartupManager
    {
        private const string RunKey = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string AppName = "WinSpotlight";

        public static void EnsureStartupRegistration()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKey, true) ?? Registry.CurrentUser.CreateSubKey(RunKey);
                if (key == null)
                {
                    return;
                }

                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    exePath = Path.Combine(AppContext.BaseDirectory, "WinSpotlight.exe");
                }

                var current = key.GetValue(AppName) as string;
                if (!string.Equals(current, exePath, StringComparison.OrdinalIgnoreCase))
                {
                    key.SetValue(AppName, exePath);
                }
            }
            catch (Exception ex)
            {
                LogService.Logger.Error(ex, "Failed to set startup registration");
            }
        }
    }
}
