# WinUI 3 migration

`UI` is now a native WinUI 3 packaged application. `App.xaml.cs` starts
`Views/MainWindow.xaml`; the old WinForms entry point, custom-drawn controls,
and XAML island host have been replaced. `TitleBar` remains the reusable XAML
menu control. Business and Data services were preserved.

## Build and run

Requirements: .NET SDK 10, WinApp CLI 0.7+, and Windows Developer Mode.
Choose **UI** as the Visual Studio startup project and **x64** as the platform.

From this directory in PowerShell:

```powershell
dotnet build .\Tomography.sln -p:Platform=x64
winapp run .\UI\UI.csproj --arch x64 --detach
```

The solution also contains ARM64 configurations; only x64 was verified on
this machine. Use project-mode `winapp run` for packaged activation.

## Preserved presentation behavior

- Four workspace regions: reconstruction, camera, waveform, acquisition data.
- Device, mode, algorithm, AI, and account parameter sections.
- EMT/EIT/ECT selection updates the mode badge and survives section changes.
- Ctrl+B, View menu, and repeated device navigation toggle the parameter panel.
- Drag the divider to resize the panel; focused divider also accepts Left/Right.
- Native title bar, system menu, help dialog, and window caption buttons.
- WinUI theme resources follow the system theme; the manifest enables PerMonitorV2 DPI awareness.

The source interface was a presentation prototype: connection, reconstruction,
AI, login, import, and export actions were disabled. These remain placeholders;
this migration does not connect the Business/Data services to the interface or
claim hardware acquisition validation.

## Verification

```powershell
.\ui-tests.ps1
```

The script runs local UI Automation and writes screenshots and `test-results.json`
under `UI/Preview/WinUI3`. It needs an unlocked desktop and brings the test app
to the foreground. The report covers 11 interaction scenarios, including both
pointer and keyboard divider resizing and native menus/dialogs. The verified
screenshots use the current system dark theme; Light and Contrast themes and
Native AOT were not tested.

Some automation shells omit `PROCESSOR_ARCHITECTURE`, causing SDK packaging tools
to select x86 binaries. The test script supplies the process architecture for
its own execution and restores the previous value afterward. If building from
such a shell on x64, set `$env:PROCESSOR_ARCHITECTURE = 'AMD64'` before building.

The original source, including the pre-existing uncommitted edits, is backed up
at `../SerialPort-before-WinUI3-20261003.zip`. `UI/Preview/workspace*.png` are the
older WinForms reference images.
