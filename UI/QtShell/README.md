# Qt main-window shell

Qt/PySide6 owns the top-level frameless window, title controls, resizing and tray menu. The existing WinUI window is hosted with `QWindow.fromWinId` / `QWidget.createWindowContainer`; individual pages remain WinUI controls.

The application starts `main.py` using `python.exe` next to the selected Python runtime DLL. Install `UI/QtShell/requirements.txt` into that interpreter before deployment. The shell uses only standard streams for lifecycle/theme messages and writes no logs or test output. If the shell cannot start, the existing window remains available with an error message.

The close button hides the Qt window to the tray. Use the tray menu to reopen or exit. File/folder pickers use the Qt top-level HWND as their owner. No Python MCP commands or model credentials are passed to the shell.
