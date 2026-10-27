# Release review: 1.0.0

Initial public release, reviewed on Windows 11 25H2 x64 (build 26200).

- Built from the source in this repository using the Windows .NET Framework compiler.
- Fresh defaults contain no personal app list, paths, screenshots, account tokens, or credentials.
- Explicit asInvoker manifest: no requested administrator elevation.
- No network listener, telemetry, remote commands, automatic downloads, or updater.
- Release ZIP includes only the app, its required icon if any, documentation, and SHA-256 hashes. No debug symbols, caches, settings, backups, or development tools.
- Source and binaries scanned for the original machine's username, private path prefixes and common credential patterns.
- Standalone hide/show test passed and restored original desktop visibility.
- Windhawk source compiled with Windhawk's compiler. Its tray worker and visibility logic passed a standalone native harness test, including restoration on unload. A catalog install/reload remains subject to Windhawk maintainer review; the harness is not a full installed-mod test.

This review is not an independent audit or a guarantee against vulnerabilities. Builds are unsigned. See SECURITY.md for reporting and limitations.
