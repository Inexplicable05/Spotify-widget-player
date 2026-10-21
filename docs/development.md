# Development

## Toolchain

Use Windows and the .NET 8 SDK (`dotnet --list-sdks`). The application targets `net8.0-windows10.0.19041.0` and uses WPF/WinRT. No Python environment, Node runtime, browser extension or service API credentials are needed.

From the repository root:

```powershell
dotnet restore SpotiFloat/SpotiFloat.csproj
dotnet build SpotiFloat/SpotiFloat.csproj -c Release
dotnet run --project SpotiFloat/SpotiFloat.csproj
```

If your SDK is installed outside PATH, put its directory on PATH or invoke its `dotnet.exe` explicitly. Do not commit a machine-specific executable path.

If a running widget locks build output, close that copy or use a separate output directory:

```powershell
dotnet build SpotiFloat/SpotiFloat.csproj -c Release -o artifacts/dev
.\artifacts\dev\SpotiFloat.exe
```

## Automated checks

```powershell
dotnet run --project tests/SourcePolicyChecks
dotnet run --project tests/SettingsChecks
powershell -NoProfile -File tests/BuildScriptChecks.ps1
```

`SourcePolicyChecks` tests allowed/blocked identities, process mappings and analysis clearing. `SettingsChecks` tests settings migration, palettes/contrast, all layout/theme/mode combinations, WPF resource updates, progress geometry, native control automation and uninstall-path resolution. It renders PNG previews under its ignored build output. The checks do not invoke actual data cleanup or uninstall, and the WPF settings checks suppress preference writes.

The build-script check mocks external tools: it verifies paths and failure handling without publishing or compiling an installer. The default checks do not require a logged-in music service or audio playback. The Windows GitHub Actions workflow runs these commands. Hosted-runner desktop behaviour may differ from an interactive machine; manual checks remain necessary.

### Optional diagnostics

```powershell
# List Windows media identities and matching audio process IDs.
dotnet run --project tests/SourcePolicyChecks -- --live

# Play two quiet test tones and verify process-specific capture isolation.
dotnet run --project tests/SourcePolicyChecks -- --capture-check
```

The capture check requires an audio output device and Windows build 20348 or later. It plays audio for about two seconds. Do not put this hardware-dependent check into unattended CI.

## Manual checks before a release

- Supported desktop apps: metadata, playback, seeking and automatic session switching.
- Blocked browser/movie audio does not affect media state or visualization.
- All layouts, themes and colour modes with real artwork; include monochrome and yellow covers.
- Keyboard navigation, focus indicators, 10-300% scaling and short-screen scrolling.
- Saving/reopening Settings, startup registration, topmost/desktop and fullscreen hiding.
- Installed-app registration and removal in Windows Settings/Control Panel. Use a disposable install to test destructive actions.

## Packaging

`SpotiFloat/Setup/installer.iss` and `build_releases.ps1` build a self-contained Windows x64 setup. The `-BuildInstaller` option requires Inno Setup 6. The installer uses the stable `SpotiFloat` AppId, installs per user without elevation, and registers an uninstaller for Windows Settings/Control Panel. Keep the application version and installer version aligned.

```powershell
.\SpotiFloat\build_releases.ps1
.\SpotiFloat\build_releases.ps1 -BuildInstaller
```

Keep release EXEs/ZIPs out of Git and publish validated packages through GitHub Releases. Project and dependency licences are copied into the publish output; the publish target also copies resolved runtime-pack licences/notices. The installer includes this complete output, excluding debug symbols.

Use `powershell -NoProfile -File tests/InstallerChecks.ps1 -InstallerPath <setup.exe>` for a disposable per-user installation/removal check. This actually runs the setup and uninstaller; it refuses to overwrite an existing installation and preserves the current startup value and preference file. Installer logs remain in ignored `artifacts/`.

Brand geometry lives in [mark.svg](assets/mark.svg). `powershell -NoProfile -STA -File tools/Export-BrandAssets.ps1` regenerates the blue/cream application icon, monochrome artwork and light/dark tray icons at eight resolutions. The tray follows the Windows taskbar theme independently of the player's theme. Generated brand assets are committed so normal builds need no graphics tools.

## Repository maintenance

The [fix history](maintenance/fixes.md) records investigations and user acceptance. An implemented issue is not considered fixed until accepted. Historic phase plans and HTML mockups are not the architecture specification; current behaviour is documented in [Architecture](architecture.md).

Generated output belongs in `bin/`, `obj/`, `artifacts/` or `SpotiFloat/Setup/Output/`, all ignored. Old Python prototype caches may remain locally under ignored `app/` or `.venv/`; they are not application inputs.
