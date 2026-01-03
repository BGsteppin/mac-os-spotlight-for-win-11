using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using WinSpotlight.Services;

namespace WinSpotlight.Search
{
    public static class ResultLauncher
    {
        public static void Launch(SearchResult result)
        {
            if (result == null)
            {
                return;
            }

            switch (result.Type)
            {
                case ResultType.Application:
                case ResultType.Shortcut:
                case ResultType.File:
                case ResultType.Folder:
                    OpenShell(result.Path);
                    new IndexingService().RecordUsage(result.Path);
                    break;
                case ResultType.Url:
                    OpenShell(result.Path);
                    break;
                case ResultType.Path:
                    OpenShell(result.Path);
                    break;
                default:
                    OpenShell(result.Path);
                    break;
            }
        }

        private static void OpenShell(string path)
        {
            var info = new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            };
            Process.Start(info);
        }
    }
}
