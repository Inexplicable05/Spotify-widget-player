# Settings and theme checks

Run `dotnet run --project tests/SettingsChecks` on Windows.

Checks black/white contrast, theme transitions from a locally coloured title, toggle-off, missing artwork, layout changes, the shared menu, and scale input validation. Renders settings in all four themes, with Advanced expanded, and constrained to a short screen. PNG previews are saved under the test build's `previews` folder.

Also checks adjusted album colours on Light/Dark, album-colour availability in Glass, and halo cleanup. Renders the Glass title on white and black backdrops.

Square/Disk checks reuse the same control while changing all four themes. They cover icons, progress, visualizer/waveform brushes, album title, toggle-off, missing artwork, and settings option availability for every layout. Eight themed player previews are rendered.

Square seek-bar checks cover 0/5/50/95/100%, marker/rail centre alignment, marker/fill endpoint alignment, rounded card clipping, tooltip clearance, and tooltip dismissal at 100% and 300% size. Each visualizer bar's bottom edge must meet the idle rail top without a gap or overlap. Corresponding PNG previews are saved beside the theme renders.

The test suppresses settings persistence and does not change user preferences or startup registration. Manually verify right-click and keyboard menus, playback/dragging, saving/reopening settings, and focus navigation before accepting the changes.
