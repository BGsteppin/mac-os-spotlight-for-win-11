using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using IWshRuntimeLibrary;
using File = System.IO.File;

namespace WinSpotlight.Services
{
    public class IconService
    {
        public ImageSource? GetIcon(string path)
        {
            try
            {
                var target = ResolveShortcut(path);
                if (File.Exists(target))
                {
                    using var icon = Icon.ExtractAssociatedIcon(target);
                    if (icon != null)
                    {
                        return Imaging.CreateBitmapSourceFromHIcon(icon.Handle, System.Windows.Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
                    }
                }
            }
            catch
            {
            }
            return null;
        }

        public string ResolveShortcut(string path)
        {
            if (Path.GetExtension(path).Equals(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var shell = new WshShell();
                    var shortcut = (IWshShortcut)shell.CreateShortcut(path);
                    if (!string.IsNullOrWhiteSpace(shortcut.TargetPath))
                    {
                        return shortcut.TargetPath;
                    }
                }
                catch
                {
                }
            }

            return path;
        }
    }
}
