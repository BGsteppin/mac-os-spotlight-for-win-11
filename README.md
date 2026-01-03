# WinSpotlight (macOS Spotlight clone for Windows 11)

This project is a Windows 11 WPF application that mirrors macOS Spotlight: translucent floating search UI, ultra-fast Everything-backed search, keyboard-first controls, and a global **Win+Space** hotkey.

## What you’re building
- Floating acrylic-like launcher centered in the top third of the primary display
- Instant search across Start Menu shortcuts, key user folders, and Everything index with SQLite fallback
- Global **Win+Space** hotkey that shows/hides the window without stealing focus
- Launch support for apps, shortcuts, files/folders, URLs, and typed paths
- Silent autostart via registry with background hotkey active immediately

## Project structure
```
Spotlight/
  src/
    Spotlight/
      App.xaml               # WPF app bootstrap
      App.xaml.cs            # Startup, single-instance guard, startup registration
      MainWindow.xaml        # Spotlight-like UI
      MainWindow.xaml.cs     # UI logic + hotkey toggling + search binding
      Interop/
        DwmApi.cs            # Mica/backdrop configuration
      Search/
        EverythingSearch.cs  # Everything SDK wrapper
        IndexingService.cs   # SQLite fallback indexer
        SearchCoordinator.cs # Multi-source ranking
        SearchResult.cs      # Result model
        ResultLauncher.cs    # ShellExecute launcher
        ShortcutCatalog.cs   # Start Menu/shortcut discovery
      Services/
        Debouncer.cs         # Typing debounce helper
        HotkeyManager.cs     # Low-level keyboard hook for Win+Space
        IconService.cs       # Shortcut resolution + icon extraction
        LogService.cs        # Rotating file logger
        SingleInstanceGuard.cs # Mutex guard
        StartupManager.cs    # Registry Run entry helper
      Spotlight.csproj
README.md
```

## Dependencies
Install .NET 8 SDK and Everything (voidtools) on Windows 11.

NuGet/COM references:
- `Microsoft.Data.Sqlite`
- `System.Drawing.Common`
- `CommunityToolkit.Mvvm`
- `Microsoft.Extensions.Logging`
- `IWshRuntimeLibrary` (COM reference from Windows Script Host)

### Install dependencies
```powershell
winget install Microsoft.DotNet.SDK.8
# Ensure Everything is installed (https://www.voidtools.com/)
```
Restore packages:
```powershell
cd Spotlight/src/Spotlight
dotnet restore
```

## Build & run
```powershell
cd Spotlight/src/Spotlight
dotnet build -c Release
dotnet run -c Release
```
Run the produced `WinSpotlight.exe` from `bin/Release/net8.0-windows`.

## Everything SDK setup
1. Install Everything desktop app.
2. Copy `Everything64.dll` from the Everything install folder (typically `C:\Program Files\Everything`) into the same directory as `WinSpotlight.exe`.
3. The app auto-detects the DLL at runtime. If missing, it transparently falls back to the SQLite indexer.

## Startup registration
The app automatically writes the Run key at launch:
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run\WinSpotlight` → full path to `WinSpotlight.exe`.

You can also register manually via PowerShell:
```powershell
$exe = "$(Split-Path -Parent $MyInvocation.MyCommand.Definition)\WinSpotlight.exe"
reg add "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v WinSpotlight /t REG_SZ /d "$exe" /f
```
Remove startup:
```powershell
reg delete "HKCU\Software\Microsoft\Windows\CurrentVersion\Run" /v WinSpotlight /f
```

## Zero-to-running (non-technical)
1. Install .NET 8 SDK (link above).
2. Install Everything and enable its service.
3. Download this project and open PowerShell in the project folder.
4. Run `cd Spotlight/src/Spotlight` then `dotnet build -c Release`.
5. Copy `Everything64.dll` next to `WinSpotlight.exe` (Release output folder).
6. Double-click `WinSpotlight.exe` to start (it will hide to the background).
7. Press **Win+Space** to open; type to search; Enter launches; Esc hides.
8. Reboot to confirm it starts silently with the hotkey active.

## Troubleshooting FAQ
- **Win+Space not triggering**: Ensure other launchers/IME do not capture Win+Space. Try running the app elevated once so the low-level hook loads, then restart. Confirm `WinSpotlight.exe` is running in Task Manager.
- **Everything not detected**: Copy `Everything64.dll` next to the exe and ensure the Everything service is running. The app will still work using the built-in indexer but results may be slower until indexing finishes.
- **Icons missing**: Verify `WinSpotlight.exe` has access to the shortcut target. Rebuild with `dotnet build` to regenerate COM interop for `IWshRuntimeLibrary`.
- **App not starting on boot**: Check `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` for `WinSpotlight`. If missing, run the PowerShell command above. Also confirm that Windows didn’t block the exe (right-click → Properties → Unblock).
- **Slow results initially**: The SQLite fallback indexes user folders in the background on first run. Leave the app running; subsequent queries use the saved index and recent-file boosting.

## Why Option B (C# WPF)
WPF on .NET 8 provides native acrylic/Mica support, straightforward global keyboard hooks, and first-class Everything DLL interop for reliable Win+Space handling on Windows 11.
