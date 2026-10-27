# Desktop Icons Toggle

Show or hide all desktop icons with a tray button or Ctrl+Alt+H. Toggle again to bring them back. This is a visual switch for a clean desktop behind transparent windows; it does not delete files or minimize running applications.

## Standalone Windows app

Windows 11 x64 and .NET Framework 4.8. Extract the release ZIP and run DesktopIconsToggle.exe as a normal user. Left-click its tray icon to toggle visibility. Right-click for the menu. If Ctrl+Alt+H is already used, the app tries Ctrl+Alt+F12; the actual shortcut appears in the menu.

Choose **Show desktop icons and exit** to restore icons and stop. Windows may retain the last icon-visibility setting if the app crashes or Windows shuts down; the desktop context menu → View → Show desktop icons always works independently.

Startup is optional: place a shortcut to DesktopIconsToggle.exe in Win+R → shell:startup. Remove the shortcut to stop launching at sign-in. No service or scheduled task is installed.

Command line: --toggle, --stop. Opening a second instance also toggles icons. --stop stops the helper without changing the current visibility.

## Windhawk alternative

The windhawk/desktop-icons-toggle.wh.cpp file provides a separate Windhawk implementation with the same hotkey and tray toggle. Use either the standalone app or the Windhawk mod, not both. Until accepted into Windhawk's official catalog, it can be installed as a local mod through Windhawk's editor. The public catalog entry is subject to maintainers' review.

## Build and test

Run ./build.ps1 on Windows. Output is in dist/. No build dependencies are downloaded. The standalone --test option briefly toggles icons both ways and restores the original state; run it only on an interactive desktop.

## Privacy and uninstall

See PRIVACY.md and SECURITY.md. The tool has no network access or telemetry. To uninstall the standalone version, exit it, remove its optional Startup shortcut, and delete the extracted folder. Optional status/error files are in %LOCALAPPDATA%\DesktopIconsToggle. To uninstall the mod, use Windhawk's Remove button.

MIT licensed. By merpzzz.
