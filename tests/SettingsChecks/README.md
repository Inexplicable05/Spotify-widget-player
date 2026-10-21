# Settings and album-colouring checks

Run `dotnet run --project tests/SettingsChecks` on Windows.

Checks all 36 combinations of Bar/Square/Disk, Dark/Light/Glass and Off/Title/Accents/Full, using two artworks on the same live controls. Verifies title/icon/progress/visualizer colours, Glass transparency and title halo cleanup, missing-art fallback, layout changes, and isolation from Settings resources. Generates 72 player previews and 12 settings previews under the test build's `previews` directory.

Checks 216 neutral contrast samples and 432 adjusted accents, including preservation of already-readable colours. Palette checks cover unchanged solid colours, dominant colour groups, transparency, and RGB24 conversion. JSON checks cover migration from the old Dynamic theme/title toggle, defaults, invalid modes and round trips without changing files or registry entries.

Full Bar checks cover 648 palettes (216 artwork colours across three themes). Verify circle/body contrast of at least 2:1 over black and white backdrops, opaque local button surfaces, icon/circle contrast of at least 4.5:1, actual resource bindings in all three button templates and clearing on mode changes. Render monochrome/yellow examples on both backdrops. These backdrop checks do not guarantee circle/body separation against every intermediate Glass wallpaper colour or pattern.

Retains shared context-menu, percentage input validation and short-screen scrolling checks. Square seek-bar checks cover 0/5/50/95/100%, marker/rail centre alignment, marker/fill endpoint alignment, rounded clipping, tooltip clearance and dismissal at 100% and 300% size. Every visualizer bar bottom must meet the idle rail top without a gap or overlap.

The test suppresses preference persistence. Manually verify mode selection with real album covers, saving/reopening Settings, keyboard navigation, and Glass on your wallpaper before accepting the changes.
