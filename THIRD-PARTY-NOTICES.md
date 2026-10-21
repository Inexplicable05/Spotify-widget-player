# Third-party notices

The project licence does not replace dependency licences. The following packages are referenced directly or resolved transitively by the current application build. Versions come from the project and NuGet restore metadata.

| Component | Version | Licence / notice |
|---|---|---|
| [NAudio](https://github.com/naudio/NAudio), including Asio, Core, Midi, Wasapi, WinForms and WinMM packages | 2.2.1 | MIT; Mark Heath and contributors. [Bundled licence](docs/licenses/NAudio-MIT.txt); component packages identify copyright Mark Heath 2023. |
| [Hardcodet.NotifyIcon.Wpf](https://github.com/hardcodet/wpf-notifyicon) | 2.0.1 | MIT; Philipp Sumi. [Bundled licence](docs/licenses/Hardcodet-MIT.txt). |
| Microsoft.NETCore.Platforms | 3.1.0 | MIT; .NET Foundation and Contributors. |
| Microsoft.Win32.Registry | 4.7.0 | MIT; .NET Foundation and Contributors. |
| System.Security.AccessControl | 4.7.0 | MIT; .NET Foundation and Contributors. |
| System.Security.Principal.Windows | 4.7.0 | MIT; .NET Foundation and Contributors. |

Microsoft support-package licence: [DotNet-MIT.txt](docs/licenses/DotNet-MIT.txt). Their original bundled notices are retained below; these upstream files can include notices for a wider codebase than the subset used here.

- [microsoft.netcore.platforms 3.1.0 bundled notices](docs/licenses/microsoft.netcore.platforms-3.1.0-notices.txt)

## Distribution

Retain the applicable copyright notices and licence texts when distributing binaries. A self-contained .NET build also ships runtime components: preserve the runtime's own licence and third-party notices from that distribution. Review this list when dependencies change.

The GitHub mark in the settings attribution links to the maintainer's profile. GitHub and music-service names/marks and album artwork remain the property of their respective owners and are not relicensed under the project's MIT licence. Album artwork is supplied at runtime by Windows media sessions; sample album artwork is not included in the repository.
