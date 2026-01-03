using System;
using System.IO;
using System.Windows;
using WinSpotlight.Services;

namespace WinSpotlight
{
    public partial class App : Application
    {
        private SingleInstanceGuard? _guard;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Directory.SetCurrentDirectory(AppContext.BaseDirectory);
            LogService.Initialize();

            _guard = new SingleInstanceGuard("WinSpotlight-Instance");
            if (!_guard.TryAcquire())
            {
                Shutdown();
                return;
            }

            StartupManager.EnsureStartupRegistration();

            var window = new MainWindow();
            window.Hide();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _guard?.Dispose();
            base.OnExit(e);
        }
    }
}
