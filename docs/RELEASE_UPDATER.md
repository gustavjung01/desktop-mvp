# CONGTY Desktop release updater

CONGTY Desktop uses a native WPF/.NET release flow. Electron updater contracts are not used.

## Key Manager profile

- Source folder: the actual clone containing `CongTy.Desktop.sln`.
- Build output: `dist\windows-release`.
- Build command: `powershell -NoProfile -ExecutionPolicy Bypass -File .\scripts\release.ps1`.
- Version file: `release.json`.
- Version field: `version`.
- Artifact patterns: `CONGTY-Setup-*.exe;latest.json`.
- Publish pointer: `latest.json`.
- R2 bucket: `hung-phat-app`.
- R2 prefix: `core/windows/stable/`.

Key Manager provides `KM_RELEASE_VERSION` and `KM_RELEASE_NOTES`. The release script rejects a version mismatch.

## Release artifacts

The release script cleans the old output and produces exactly:

- `CONGTY-Setup-<version>.exe`;
- `latest.json`.

The manifest contains schema version, latest version, release notes, relative installer path, SHA-256 and byte size. Key Manager uploads the installer first and `latest.json` last.

The public feed is:

`https://pub-381648426a2447a7a5edd970ca02d14e.r2.dev/core/windows/stable/latest.json`

No R2 write credential is stored in Desktop.

## Installed application

The installer is per-user and defaults to:

`%LOCALAPPDATA%\Programs\CONGTY`

User settings remain outside the install directory:

`%LOCALAPPDATA%\CongTy\Desktop\settings.json`

The updater only enables itself when the installed executable matches the HKCU install marker. Development builds do not self-update.

Downloads use:

`%LOCALAPPDATA%\CongTy\Desktop\Updates\<version>\`

The updater writes each download to a per-attempt partial file, validates exact size and SHA-256, then promotes it to a runnable installer. A stale or externally locked legacy `.partial` file is never deleted as a prerequisite for a new download; if the preferred local installer filename is locked, the verified payload is promoted to a unique `.exe` fallback instead. A pending marker is written before starting the silent installer. Update success is shown only after restart when the running assembly version exactly matches the requested target version and differs from the previous version.

CI builds and verifies release artifacts offline. It also silently installs the generated installer into an isolated per-user CI directory, verifies the HKCU install/uninstall markers and packaged version, launches the installed executable with `--installed-package-smoke` to confirm updater installed-build detection, then silently uninstalls and verifies cleanup. CI never uploads to production R2.
