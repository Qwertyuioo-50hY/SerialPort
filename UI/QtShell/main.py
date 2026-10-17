"""Qt owns the frameless top-level window; existing WinUI pages stay embedded."""
import argparse
import ctypes
from ctypes import wintypes
import json
import sys
import threading
from pathlib import Path


def emit(event, **values):
    print(json.dumps(dict(event=event, **values)), flush=True)


def main():
    from PySide6.QtCore import Qt, QObject, Signal, QTimer, QRectF
    from PySide6.QtGui import QIcon, QWindow, QCursor, QPainterPath, QRegion
    from PySide6.QtWidgets import QApplication, QWidget, QVBoxLayout, QSystemTrayIcon, QMenu

    parser = argparse.ArgumentParser()
    parser.add_argument("--window", required=True, type=int)
    parser.add_argument("--theme", default="Light")
    args = parser.parse_args()
    user32 = ctypes.WinDLL("user32", use_last_error=True)
    user32.IsWindow.argtypes = [wintypes.HWND]
    user32.IsWindow.restype = wintypes.BOOL
    user32.SetForegroundWindow.argtypes = [wintypes.HWND]
    user32.ShowWindow.argtypes = [wintypes.HWND, ctypes.c_int]
    long_type = ctypes.c_ssize_t
    get_style = user32.GetWindowLongPtrW
    get_style.argtypes = [wintypes.HWND, ctypes.c_int]
    get_style.restype = long_type
    set_style = user32.SetWindowLongPtrW
    set_style.argtypes = [wintypes.HWND, ctypes.c_int, long_type]
    set_style.restype = long_type
    user32.SetWindowPos.argtypes = [wintypes.HWND, wintypes.HWND, ctypes.c_int, ctypes.c_int, ctypes.c_int, ctypes.c_int, wintypes.UINT]
    if not user32.IsWindow(args.window):
        raise RuntimeError("WinUI 页面窗口已失效。")
    app = QApplication(sys.argv[:1] + ["-platform", "windows:dpiawareness=2"])
    ctypes.windll.shell32.SetCurrentProcessExplicitAppUserModelID("FieldSync.Desktop")
    app.setApplicationName("FieldSync")
    app.setQuitOnLastWindowClosed(False)
    icon = QIcon(str(Path(__file__).resolve().parent.parent / "Assets" / "AppIcon.ico"))
    app.setWindowIcon(icon)

    class Commands(QObject):
        received = Signal(dict)

    class Shell(QWidget):
        def __init__(self):
            super().__init__(None, Qt.Window | Qt.FramelessWindowHint)
            self.quitting = False
            # Round the top-level window, including its hosted WinUI child.
            corner = ctypes.c_int(2)
            ctypes.windll.dwmapi.DwmSetWindowAttribute(wintypes.HWND(int(self.winId())), 33, ctypes.byref(corner), ctypes.sizeof(corner))
            self.setWindowTitle("FieldSync")
            self.setWindowIcon(icon)
            self.setMinimumSize(800, 560)
            self.resize(1440, 900)
            screen = app.primaryScreen().availableGeometry()
            self.resize(min(self.width(), screen.width()), min(self.height(), screen.height()))
            self.move(screen.center() - self.rect().center())
            layout = QVBoxLayout(self)
            layout.setContentsMargins(4, 4, 4, 4)
            layout.setSpacing(0)
            self.foreign = QWindow.fromWinId(args.window)
            if self.foreign is None:
                raise RuntimeError("Qt 无法承载现有页面。")
            self.container = QWidget.createWindowContainer(self.foreign, self)
            self.container.setFocusPolicy(Qt.StrongFocus)
            layout.addWidget(self.container, 1)
            # Only the hosted HWND loses its native frame; page controls stay intact.
            style = get_style(args.window, -16)
            style = (style & ~0x80CF0000) | 0x40000000 | 0x06000000
            set_style(args.window, -16, style)
            user32.SetWindowPos(args.window, None, 0, 0, 0, 0, 0x37)
            self.tray = QSystemTrayIcon(icon, self)
            self.tray.setToolTip("FieldSync")
            menu = QMenu()
            menu.addAction("打开 FieldSync", self.restore)
            menu.addAction("最小化", self.showMinimized)
            menu.addAction("最大化或还原", self.toggle_maximized)
            menu.addSeparator()
            menu.addAction("退出", self.quit)
            self.tray_menu = menu
            self.tray.setContextMenu(menu)
            self.tray.activated.connect(lambda reason: self.restore() if reason in (QSystemTrayIcon.Trigger, QSystemTrayIcon.DoubleClick) else None)
            self.tray.show()
            self.setMouseTracking(True)
            self.apply_theme(args.theme)
            self.monitor = QTimer(self)
            self.monitor.timeout.connect(self.check_backend)
            self.monitor.start(1000)

        def resizeEvent(self, event):
            super().resizeEvent(event)
            if self.isMaximized():
                self.clearMask()
            else:
                outline = QPainterPath()
                outline.addRoundedRect(QRectF(self.rect()), 12, 12)
                self.setMask(QRegion(outline.toFillPolygon().toPolygon()))

        def apply_theme(self, theme):
            dark = theme == "Dark"
            background, foreground, hover = ("#101010", "#EEEEEE", "#303030") if dark else ("#F4F4F6", "#202020", "#E5E5E8")
            self.setStyleSheet(f"QWidget {{ background: {background}; color: {foreground}; }} QPushButton {{ border: none; border-radius: 5px; font-size: 18px; }} QPushButton:hover {{ background: {hover}; }} QMenu {{ border: 1px solid {hover}; padding: 5px; }} QMenu::item {{ padding: 6px 18px; }} QMenu::item:selected {{ background: {hover}; }}")

        def toggle_maximized(self):
            self.showNormal() if self.isMaximized() else self.showMaximized()

        def restore(self):
            self.showNormal() if self.isMinimized() else self.show()
            self.raise_()
            self.activateWindow()
            self.foreign.show()
            user32.SetForegroundWindow(int(self.winId()))

        def closeEvent(self, event):
            if self.quitting:
                event.accept()
                return
            event.ignore()
            if QSystemTrayIcon.isSystemTrayAvailable():
                self.hide()
            else:
                self.showMinimized()

        def mousePressEvent(self, event):
            point = event.position()
            edges = Qt.Edge(0)
            if point.x() < 6: edges |= Qt.LeftEdge
            if point.x() >= self.width() - 6: edges |= Qt.RightEdge
            if point.y() < 6: edges |= Qt.TopEdge
            if point.y() >= self.height() - 6: edges |= Qt.BottomEdge
            if event.button() == Qt.LeftButton and edges and not self.isMaximized():
                self.windowHandle().startSystemResize(edges)

        def mouseMoveEvent(self, event):
            point = event.position()
            horizontal = point.x() < 6 or point.x() >= self.width() - 6
            vertical = point.y() < 6 or point.y() >= self.height() - 6
            self.setCursor(QCursor(Qt.SizeFDiagCursor if horizontal and vertical else Qt.SizeHorCursor if horizontal else Qt.SizeVerCursor if vertical else Qt.ArrowCursor))

        def command(self, message):
            name = message.get("command")
            if name == "show": self.restore()
            elif name == "hide": self.close()
            elif name == "move": self.windowHandle().startSystemMove()
            elif name == "maximize": self.toggle_maximized()
            elif name == "theme": self.apply_theme(message.get("theme", "Light"))
            elif name == "quit": self.quit()

        def check_backend(self):
            if not user32.IsWindow(args.window): self.quit()

        def quit(self):
            if self.quitting: return
            self.quitting = True
            self.tray.hide()
            if user32.IsWindow(args.window):
                self.foreign.setParent(None)
                user32.ShowWindow(args.window, 0)
            emit("exit")
            app.quit()

    shell = Shell()
    commands = Commands()
    commands.received.connect(shell.command)

    def read_commands():
        for line in sys.stdin:
            try:
                value = json.loads(line)
                if isinstance(value, dict): commands.received.emit(value)
            except (ValueError, TypeError):
                continue
        commands.received.emit({"command": "quit"})

    threading.Thread(target=read_commands, daemon=True).start()
    shell.show()
    shell.foreign.show()
    emit("ready", window=int(shell.winId()))
    return app.exec()


if __name__ == "__main__":
    try:
        sys.exit(main())
    except Exception:
        emit("error", message="Qt 主窗口启动失败，请确认所选 Python 已安装 PySide6。")
        sys.exit(1)
