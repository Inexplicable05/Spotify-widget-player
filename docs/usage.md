# Usage and troubleshooting

## Controls

Start a supported desktop music app, play a track, then launch SpotiFloat. Drag the widget to move it; enable **Lock position** when you are happy with its position. Square and Disk reveal their playback controls on hover. Use their progress control to seek.

Right-click the widget or its tray icon for **Settings** and **Exit**. Settings also has Refresh media and Exit icons beside Appearance; their tooltips identify the actions. The header's close button closes Settings only.

## Appearance

Choose Bar, Square or Disk; Dark, Light or Glass; and an independent Album colouring mode. Off follows the base theme, Title changes the title, Accents adds coloured controls/visualizers, and Full adds a tinted surface. Colours can be adjusted for readability, so the displayed accent is not always the exact source pixel.

Size accepts 10-300%; the size-only Reset returns to 100%. There is no general reset button. Appearance updates immediately and preferences save automatically.

**On top** keeps the widget above other windows. **Desktop** puts it below normal windows; it is not an embedded wallpaper component. Hide during fullscreen applies only to On top and is disabled when Desktop is selected. Start with Windows registers the executable's current location, so enable it from the copy you intend to keep.

## No track appears

- Confirm playback is in Spotify, Apple Music, Amazon Music, Deezer or TIDAL's desktop app.
- Browser sessions, including YouTube, YouTube Music and browser Spotify, are deliberately ignored.
- Check that Windows itself sees the app's media session. Some versions or playback situations expose incomplete state.
- Use Refresh media or restart the music app and widget.

The widget selects an eligible session automatically. It does not currently offer a source-picker UI. Media-command and timeline support depend on the music app.

## Visualizer is idle

The visualizer requires Windows build 20348 or later, active playback and a matching active audio process. Windows 11 satisfies the OS requirement. Earlier Windows builds, protected/unsupported capture, a changed audio-host executable or an unavailable audio device may leave the visualizer idle.

Other apps and browser audio do not feed the visualizer. There is no full-system audio fallback. Pausing stops capture and clears the analysis data.

## Artwork or colours look unexpected

Artwork comes from the Windows media session; SpotiFloat cannot improve a low-resolution source thumbnail. Album-colour selection favours a representative colour group. Monochrome covers naturally produce neutral accents. Contrast treatment can lighten or darken colours; transparent Glass also depends on the desktop behind it.

Rectangular artwork in the rotating Disk layout still needs broader testing. See the [maintenance history](maintenance/fixes.md) for recorded issues and acceptance status.

## Preferences, startup and removal

Preferences: `%APPDATA%\SpotiFloat\settings.json`.

Startup entry: `HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run`, value `SpotiFloat`.

Advanced contains:

- **Clear data and exit:** removes saved preferences and startup registration, then exits. It leaves app files in place.
- **Uninstall SpotiFloat:** available only when the running copy matches a registered installation and its uninstaller exists. It opens the registered uninstaller after confirmation. Portable/development copies show an explanation and a disabled action.
- **Version:** identifies the app build.

The Windows x64 installer installs for the current account under `%LOCALAPPDATA%\Programs\SpotiFloat` by default and registers removal in Windows Settings and Control Panel. No separate .NET installation is required. Startup remains opt-in through Settings. Uninstall removes app files, shortcuts and the SpotiFloat startup entry; it retains saved preferences. Use **Clear data and exit** first if you want to remove those too. Older administrator-installed copies may need to be removed separately before installing this per-user release.

## Privacy

SpotiFloat reads local Windows media-session metadata, artwork and timing. For visualization it processes the selected app's audio in memory; it does not record audio to a file. Preferences are local. The app has no telemetry or music-service account authentication. The GitHub attribution button opens your browser when clicked.

Development checks can print app identities/process IDs or render song titles and artwork into local previews. Review those outputs before sharing them in an issue.
