# Contributing to SpotiFloat

Thanks for helping improve SpotiFloat. It is a Windows desktop app; use the .NET 8 SDK and read [Development](docs/development.md) for commands and [Architecture](docs/architecture.md) for the shared media path.

## Report a problem

Include your Windows build, music app/version, player layout, theme and album-colouring mode. Explain what you expected, what happened and how to reproduce it. Screenshots help with visual issues; remove private information first. Browser sources are intentionally unsupported.

For security-sensitive reports, follow [SECURITY.md](SECURITY.md) instead of posting exploit details publicly.

## Make a change

1. Discuss substantial behaviour or design changes in an issue first.
2. Keep changes focused. Reuse `MediaState`, `IPlayerControl`, `PlayerControlBase` and shared resources rather than adding a backend for each layout.
3. Add focused checks for new logic and preserve existing behaviour. Do not generate synthetic playback or use system-wide audio as a capture fallback.
4. Run the build and documented checks from the repository root. For UI work, include before/after images and note any manual checks not performed.
5. Explain the user-visible change and its verification in your pull request. Do not include generated binaries, local settings or music artwork.

Use clear C#/XAML and the existing project structure. Be respectful in discussions and reviews. Never put secrets, private listening details or personal screenshots into a report without reviewing them.

Contributions are made under the project's [MIT licence](LICENSE). Preserve third-party copyright and licence notices.
