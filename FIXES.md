# Player fixes

## Workflow

- Use Ponytail for every investigation and change.
- Verify each reported issue in the source and, when possible, at runtime. Explain the cause and reference established solutions before proposing a change.
- Implement player changes only after the user approves the proposed solution.
- Test when possible; request user testing where needed. Only the user's confirmation marks an issue fixed.
- Never commit or push without explicit user permission for those actions.
- Record the commit ID after an approved commit. An uncommitted change has no commit ID.

## 10/21 (2026)

### P001 - Disk player shows a second grey ring and an overly thick hover progress ring

- **Status:** Fixed - user accepted on 10/21; supporting player implementation approved for inclusion in the commit.
- **Reported:** Idle view shows a grey ring between the artwork and green progress arc. Hover makes the progress ring too thick.
- **Evidence:** User supplied idle and hover screenshots; inspected `SpotiFloat/Controls/DiskPlayerControl.xaml` and `.xaml.cs`.
- **Cause:** `BuildArc` creates the background track at radius 115. `UpdateArc` moves the progress arc to radius 118.5 when idle but never updates the background track geometry. The two paths therefore form separate rings. Artwork radius is 110, leaving additional space beneath the progress ring. Hover thickness jumps from 3 to 10 WPF device-independent units, over three times the idle width.
- **Options:** (A) Align both paths, move the ring closer to the artwork, and use a subtle 3-to-4 hover width with a smaller handle; (B) align both paths and keep a constant thin width, revealing the handle on hover; (C) hide the background track and display only the played arc, with a subtle track revealed during interaction.
- **Recommendation:** A. Use the same geometry for background and progress, keep a small consistent artwork gap, soften the track, and preserve a generous invisible seeking area. Exact dimensions are design proposals, not values required by a standard.
- **References:** [WPF shapes and drawing](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/shapes-and-basic-drawing-in-wpf-overview), [Fluent slider guidance](https://fluent2.microsoft.design/components/web/react/core/slider/usage/), [Material Web slider](https://github.com/material-components/material-web/blob/main/docs/components/slider.md).
- **Approval:** User approved option A on 10/21.
- **Implementation:** Both paths now use a shared radius of 114. Stroke width is 3 idle / 4 hover, leaving a 2-unit artwork gap on hover. Handle diameter reduced from 14 to 10; background track opacity reduced from 60/255 to 35/255. Existing 16-unit seeking band preserved.
- **Verification:** Release build passed with 0 warnings / 0 errors. Geometry checks passed for shared radius, artwork clearance, handle bounds, and seeking-band coverage. User visual and interaction testing required: idle ring alignment, hover appearance, dragging around the arc, and preferred widget scales.
- **User acceptance:** Approved on 10/21.
- **Commit:** `6973a72` - Add shared player layouts and fix Disk progress ring (P001).

### P002 - Unsupported media sessions take over playback metadata and controls

- **Status:** Fixed - user accepted on 10/21, including Deezer and TIDAL support.
- **Reported:** Netflix and other unwanted media appear in the widget. Allow Spotify, Apple Music, Amazon Music, and YouTube / YouTube Music in the browser; ignore other services.
- **Approved scope (10/21):** User chose to block all browser media and allow Spotify, Apple Music, and Amazon Music desktop apps; subsequently approved adding Deezer and TIDAL. No browser extension or additional user permissions required by this implementation.
- **Cause:** `UpdateCurrentSession` calls Windows `GetCurrentSession` without filtering. Visibility separately depends only on the Spotify process, so other supported players cannot independently make the widget visible.
- **Implementation:** Shared identity allowlist rejects all other sessions by default. Enumerates available sessions; prefers an allowed playing Windows-current session, then the existing playing session, then another playing session, then the existing paused session or first allowed session. Visibility follows the selected session rather than the Spotify process. Polling and session/playback events refresh selection; named handlers detach correctly. Discards asynchronous metadata if its session was replaced before completion.
- **References:** [Windows session API](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession), [Windows media properties](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmediaproperties), [Chrome tabs API](https://developer.chrome.com/docs/extensions/reference/api/tabs).
- **Verification:** Expanded checks pass for 15 allowed / 18 blocked identities, process mappings, and pause/blocked-source clearing. Live read-only session check allowed `Spotify.exe` and blocked `OperaSoftware.OperaWebBrowser.1772665278`. Separate-output Release build passed with 0 warnings/errors. Live playback/capture for Apple Music, Amazon Music, Deezer, and TIDAL still needs testing on installations of those apps. Run checks with `dotnet run --project tests/SourcePolicyChecks -- --live`.
- **Manual acceptance checks:** Restart the source build; play Spotify, then browser YouTube/Netflix and confirm metadata/buttons remain on Spotify. With no allowed session, the widget should hide. Test Apple Music and Amazon Music independently without Spotify running, plus switching between allowed apps.
- **Limit:** Desktop identity variants require verification on each supported app installation; unknown identities are rejected. System-wide audio visualization has been replaced under P005.
- **User acceptance:** Approved on 10/21.
- **Commit:** `a6ab762` - Filter music sources and isolate visualizer audio per application.

### P003 - Landscape thumbnail leaves exposed artwork corners in Disk layout

- **Status:** Open - source inspection consistent with screenshot; awaiting solution approval. Not fixed.
- **Reported:** YouTube thumbnail appears as a misaligned rotating rectangle inside the circular player. User clarified that Square was a wording mistake; this issue concerns Disk.
- **Cause:** Disk rotates the `Image` directly, while only the outer `DiscGrid` applies a circular clip. The image has no explicit square viewport or crop before rotation. Rectangular artwork/layout clipping can rotate through the circle instead of presenting a fully covered disc. Shared artwork assignment accepts the provided thumbnail unchanged. A video thumbnail is the artwork supplied by the media session, not necessarily a separate album cover.
- **Proposed approach:** Center-crop artwork into a fixed square viewport before rotating that viewport and retain the outer circular clip. Preserve aspect ratio; do not stretch thumbnails. Confirm with landscape, portrait, and square artwork at several rotation angles. Square changes are outside this issue's scope.
- **References:** [WPF imaging and UniformToFill](https://learn.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/imaging-overview), [WPF transforms](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/transforms-overview).
- **Verification:** Source inspection only; reproduce with rectangular test images before implementation.
- **User acceptance:** Pending.
- **Commit:** None.

### P012 - Gap between Square visualizer baseline and progress bar

- **Reported / approved:** 10/21. User requested moving the visualizer down to touch the top of the progress bar.
- **Cause:** Visualizer canvas had a 7-unit bottom margin above the 3-unit idle progress rail, leaving a 4-unit gap. Bars already anchor to the canvas bottom.
- **Follow-up 10/21:** User screenshot still showed a gap. Process inspection confirmed the running app was `bin/SquareMarkerCheck/SpotiFloat.exe`, the build before this adjustment. Corrected build is `bin/SquareVisualizerCheck/SpotiFloat.exe`. Added checks for every individual bar's bottom edge meeting the rail top, not only the canvas baseline. No further position adjustment: lowering the corrected baseline again would overlap the rail.
- **Implementation:** Reduced the existing canvas bottom margin to 3 units so its baseline meets the idle rail top. Reused the current WPF layout; no audio or seeking changes.
- **Status:** Fixed - user accepted on 10/21; committed.
- **Verification:** WPF layout checks passed for visualizer baseline touching the idle rail at 100% and 300% scale. Existing progress/theme/settings checks passed. Release build passed with 0 warnings/errors. User confirmed the corrected build's live appearance. Additional per-bar checks were rerun successfully before committing: every bar bottom meets the rail top at 100% and 300% scale.
- **User acceptance:** Approved on 10/21 ("perfect").
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.

### P009 - Album-coloured title must work in Light, Dark, and Glass

- **Status:** Implemented - awaiting user acceptance. Not yet confirmed fixed.
- **Reported:** Album-coloured title appears to work only in Light; user requires it in Dark and Glass as well.
- **Cause:** P008 explicitly disabled the option for Glass and rejected non-opaque backgrounds. Dark permits accents only when the original extracted colour already reaches 4.5:1 contrast, otherwise falling back to theme text. This can make the feature appear inactive for dark artwork. These restrictions were introduced by the previous readability fix.
- **Approval:** User approved the brightness adjustment and Glass halo on 10/21.
- **Implementation:** Light/Dark keep an already-readable album colour, otherwise blend it toward a readable shade until it meets 4.5:1 contrast. Glass enables the option, adjusts album colour for a black backing, and adds a small zero-offset black text halo using WPF DropShadowEffect. Theme changes, missing artwork, or disabling the option remove the halo and restore theme text. Dynamic keeps automatic black/white. This supersedes P008's Glass restriction.
- **Verification:** Tests passed for 432 adjusted accents and preservation of already-readable colours, red artwork hue in all three static themes, option availability, halo cleanup, Dynamic transitions, and existing settings checks. Inspected Glass title renders on white and black backdrops. Separate-output Release build passed with 0 warnings/errors. The older SettingsCheck executable was locked by the running widget; new build is in `SpotiFloat/bin/AlbumTitleCheck`.
- **Limit:** Glass remains transparent; the subtle halo is a readability aid, not a guarantee against every wallpaper. User visual acceptance on actual wallpaper remains pending.
- **User acceptance:** Pending.
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.

### P004 - Browser media thumbnails look blurry compared with album artwork

- **Status:** Open - cause not yet verified. Not fixed.
- **Reported:** Album covers appear sharp, but thumbnails from other media are blurry.
- **Evidence:** Disk requests `HighQuality` bitmap scaling. The shared loader decodes the thumbnail provided by the Windows media session without an explicit decode-size reduction. It does not fetch a higher-resolution original.
- **Hypothesis:** A smaller or more compressed browser-provided thumbnail is being enlarged by widget scaling and center cropping. Incorrect artwork layout may compound the appearance. Source pixel dimensions and displayed dimensions must be measured before attributing the blur to source quality.
- **Proposed investigation:** Compare decoded thumbnail dimensions with the displayed disc size and reproduce with a known high-resolution landscape image. Correct layout first; if the source itself lacks detail, interpolation cannot recover it. Do not add a web fetch or browser extension without approval.
- **User acceptance:** Pending.
- **Commit:** None.

### P005 - Square and Disk visualizers react to blocked apps' audio

- **Status:** Fixed - user accepted on 10/21.
- **Reported:** Browser media no longer takes over player metadata/controls, but still drives Square and Disk visualizers.
- **Cause:** `AudioProcessor` uses system-wide `WasapiLoopbackCapture`, independent of the selected media session. Its real-capture path does not gate on `IsPlaying`; that flag currently gates only simulated data. All apps' output is mixed before FFT/waveform processing.
- **Recommended solution:** Capture only the selected supported music app's process tree using Windows application loopback. Reuse existing FFT/waveform processing and both layouts. Resolve the selected app to its audio process, restart capture on source changes, clear/decay visualization when paused or unavailable, and stop generating simulated music when capture fails. Never fall back to whole-device capture, which would reintroduce browser audio.
- **Approval:** User approved application-specific capture on 10/21.
- **Implementation:** Windows application-loopback activation uses the existing NAudio 2.2.1 audio interfaces. Resolves the selected supported app to an active Windows audio-session process, captures that process and its children, and reuses the FFT/waveform processing. Cancels old capture and clears data on source changes/pause. Both layouts now consume real capture only; simulation removed. Capture errors and unsupported systems leave visualization quiet. All application exit paths cancel capture.
- **Alternative:** Gate whole-device capture on the selected app's playing state. Smaller but incomplete: browser sounds still contaminate the visualization while music plays simultaneously.
- **Compatibility:** Microsoft application loopback requires Windows build 20348 or later. Current project still targets Windows build 19041; older systems retain playback controls but quiet visualizers. No dependency upgrade. Audio owners are matched by executable name; differently named host processes or unsupported identity variants fail quietly instead of capturing system audio.
- **Reference:** [Microsoft application loopback sample](https://learn.microsoft.com/en-us/samples/microsoft/windows-classic-samples/applicationloopbackaudio-sample/).
- **Verification plan:** Browser-only audio with music paused produces no animation; simultaneous browser/music visualization follows only music; source switching, app exit, unavailable capture, and both layouts work correctly.
- **Automated verification:** Separate-output Release build passed with 0 warnings/errors. A real capture check ran two simultaneous tone-generator processes: 94,080 target samples captured; target-frequency magnitude 0.009839 versus excluded-process frequency 0.000003. Source/pause checks also passed. Tests operate on memory buffers, not recordings saved to disk.
- **Verification limits:** Agent did not verify end-to-end playback for every supported app or audio-device changes; the user accepted both fixes on 10/21. The live Spotify session had no active audio process during agent inspection.
- **User acceptance:** Approved on 10/21.
- **Commit:** `a6ab762` - Filter music sources and isolate visualizer audio per application.

### P006 - Player surface has no right-click Settings / Exit menu

- **Status:** Implemented - awaiting user acceptance. Not yet confirmed fixed.
- **Reported:** Users should be able to right-click the player to open Settings or exit without discovering the tray icon.
- **Cause:** `MainWindow.xaml` defines a context menu only on `TaskbarIcon`; neither Window nor PlayerHost has one. Settings contains an Exit App button, but no direct player-surface exit action exists.
- **Recommendation:** Add the standard WPF ContextMenu to the shared player host with Settings, a separator, and Exit. Reuse the existing handlers for all three layouts, preserve left-click seeking/dragging, and verify keyboard menu access.
- **Approval:** User approved implementation on 10/21.
- **Implementation:** PlayerHost now has Settings / Exit menu entries wired to the existing handlers and is focusable for keyboard access. Menu belongs to the host, so switching layouts keeps it.
- **Verification:** WPF checks confirm menu entries on Bar, Square, and Disk and a focusable host. Live right-click, keyboard invocation, seeking, and dragging remain for user testing.
- **User acceptance:** Pending.
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.

### P007 - Settings panel is visually bulky and inconsistent

- **Status:** Implemented - awaiting user acceptance. Not yet confirmed fixed.
- **Reported:** Settings should be sleek, minimal, and easy to use.
- **Evidence:** Most options are separate bordered cards with 15-unit padding and 20-unit bottom margins. Layout/theme controls use additional framed containers; large header/footer, many icons, mixed control styles, and hidden scrollbars add visual weight or reduce discoverability. The Draggable Widget label maps to IsLocked, reversing the apparent meaning of its checked state. The uninstall description also overstates removal: its handler deletes preferences/startup registration and shuts down, not the installed executable.
- **Recommendation:** One compact panel with Appearance and Behavior groups, unframed aligned rows, consistent spacing/type/control heights, compact layout/theme choices, and visible scrolling only when needed. Use standard size slider plus numeric percent, label the position setting Lock position, and put recovery/cleanup actions behind an Advanced expander. Keep a clear Close action, normal keyboard focus, and readable theme-aware text. Preserve all existing preferences and avoid new settings/dependencies.
- **Approval:** User approved applying the compact redesign on 10/21.
- **Implementation:** Replaced per-setting cards with Appearance / Behavior rows, labelled radio choices, size slider and editable 10-300% input, correctly named Lock position, and Advanced recovery actions. Removed custom drag-to-edit size logic and hidden scrolling. Uses existing settings fields and persistence. Cleanup wording now states that app files remain. Fullscreen/title-colour options explain or disable themselves when inapplicable.
- **Verification:** WPF checks passed for finite/clamped percentage input, Lock position label, four theme renders, and scrolling on a short window. Visually inspected Dark, Light, Glass, Dynamic, expanded Advanced, and short-screen previews. Test harness suppresses preference/registry writes. Saving/reopening settings and keyboard interaction need user testing.
- **Reference:** [Fluent layout guidance](https://fluent2.microsoft.design/layout).
- **User acceptance:** Pending.
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.

### P008 - Old album accent title persists after switching to Dynamic theme

- **Status:** Implemented - awaiting user acceptance. Not yet confirmed fixed.
- **Reported:** A red title from Light theme with Dynamic Title Colour enabled remains red after selecting Dynamic theme, even when later album colours make it unreadable.
- **Cause:** `UpdateMediaState` assigns a local SolidColorBrush to Bar TitleText.Foreground, replacing its dynamic resource expression. `ApplyDynamicTheme` updates PrimaryTextForeground but never restores the title's resource reference. The same stale local brush can persist when accent title colour is disabled. Current surface brightness uses a simple weighted sRGB threshold rather than a contrast-ratio calculation.
- **Recommendation:** Restore the title's theme resource reference when leaving accent-title mode, entering Dynamic theme, or artwork is unavailable. Dynamic theme should choose black/white text by calculated contrast against the actual surface; keep album colour on surfaces/accents. If coloured titles remain available in static themes, use the accent only when its contrast is sufficient, otherwise fall back to readable theme text. Check theme transitions, toggle-off, missing artwork, and layout switches.
- **Approval:** User approved implementation on 10/21.
- **Implementation:** Shared theme application restores the title resource expression before applying any eligible accent. Dynamic text uses WCAG relative luminance to choose black/white. Static album accents require 4.5:1 contrast on an opaque surface; Glass uses theme text because the wallpaper cannot be measured. Dynamic button/tooltip/setting surfaces keep text contrast. Theme selection, title toggle, missing artwork, and layout switching refresh appearance immediately.
- **Verification:** WPF checks passed for 216 black/white contrast samples, known 21:1 black/white ratio, red local-title to Dynamic transition, six artwork colours, dynamic control surfaces, toggle-off, missing artwork, and layout changes. Release build passed with 0 warnings/errors; diff whitespace checks passed. User playback/theme-transition acceptance remains pending.
- **References:** [WPF property precedence](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/properties/dependency-property-value-precedence), [WCAG text contrast](https://www.w3.org/WAI/WCAG21/Understanding/contrast-minimum.html).
- **User acceptance:** Pending.
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.

### P010 - Square and Disk ignore themes and album-coloured titles

- **Status:** Implemented - awaiting user acceptance. Not yet confirmed fixed.
- **Reported:** Square/Disk buttons, progress, visualizers, and text stay white across themes; album-coloured title affects only Bar. User requested a shared architectural fix rather than duplicate layout logic.
- **Cause:** Shared overlay styles and both layouts used hard-coded brushes. Disk's generated paths also assigned local brushes and an independent artwork ring colour. MainWindow title handling explicitly required Bar, while Settings disabled the option on other layouts.
- **Approval:** User requested implementation on 10/21.
- **Implementation:** Existing theme resources now drive shared overlay icons, title/subtitle styles, timestamps, tracks, thumbs, generated visualizer bars, and Disk paths. Shared overlay colour/scrim/halo provides themed surfaces over artwork. `IPlayerControl.ApplyTitleAppearance` delegates all three layouts to one implementation in PlayerControlBase, using the existing contrast adjustment and Glass halo. MainWindow supplies the theme and extracted album colour; removed the Bar-only branch, layout restriction, and unused duplicate Disk colour extraction.
- **Reference:** [WPF reusable styles and dynamic resources](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/systems/xaml-resources-overview).
- **Verification:** Release build passed with 0 warnings/errors. WPF checks switch themes on already-created Square/Disk controls and verify icon, progress, visualizer, waveform, title, toggle-off, and missing-art behaviour. Settings option availability checked for every layout. Existing theme/contrast/input tests passed. Inspected Square/Disk hover renders in Light/Dark and Dynamic; eight player previews generated.
- **User acceptance:** Pending. Test all four themes with each layout open, plus album-title toggle and track changes.
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.

### P011 - Square progress bar is visually heavy and cramped at the card edge

- **Status:** Implemented - awaiting user acceptance. Not yet confirmed fixed.
- **Reported:** Square progress control is usable but looks too thick, with an oversized handle and crowded timestamp near the bottom edge.
- **Cause:** Idle track is 4 units high; hover track fills a 12-unit container across the full card width. The 14-unit knob sits inside a card clipped to rounded corners, with little edge clearance. Timestamp is placed relative to this edge-aligned control. Widget scaling magnifies these dimensions.
- **Options:** A: Inset rounded seek rail, 3 idle / 4 hover units, 8-unit hover handle, larger invisible hit area, and compact timestamp positioned clear of the rail/card edge. B: Keep a thin edge progress indicator while idle and reveal a separate inset seek slider on hover. C: Constant thin inset rail with no thickness change, revealing only the handle on hover.
- **Recommendation:** A, revised to follow the user's placement clarification: a single slim rail flush with the full bottom edge, using existing shared theme resources and RequestSeek. Keep the smaller handle clear of rounded corners and preserve a generous invisible interaction target.
- **Reference:** [Fluent slider anatomy and interaction](https://fluent2.microsoft.design/components/web/react/core/slider/usage/). Proposed dimensions are design choices for this widget, not prescribed Fluent values.
- **Approval:** User approved option A on 10/21, then clarified that the progress bar must remain at the very base of the player; the initial inset placement was rejected.
- **Follow-up reported 10/21:** Knob looks misaligned with the bottom rail. Confirmed: both shapes were bottom-aligned, but the 8-unit knob centre sat 2 units above the 4-unit rail centre. The 14-unit horizontal endpoint clamp also separated the knob from the fill endpoint near the start/end. Existing geometry tests checked containment, not centreline alignment. User approved correction with "fix it" on 10/21; implementation completed, acceptance pending.
- **Implementation:** Idle/hover tracks span the full width flush with the bottom edge, at 3/4 units high. Replaced the circular knob with an 8-by-4 rounded marker, aligned vertically with the hover rail and horizontally centred on its fill endpoint. Removed the independent endpoint clamp and marker shadow. Marker and rail share the existing rounded card clip, so the marker naturally disappears at the extreme endpoints rather than being shifted away from the actual position. The 24-unit invisible hit area and full-width seeking remain. Compact timestamp stays inside card edges and clear of the artist/title group. Tooltip hides when leaving the seek area except during active dragging. Existing shared theme resources and RequestSeek remain in use.
- **Verification:** WPF tests passed for bottom-flush/full-width rail geometry, exact marker/rail centre alignment and marker/fill endpoint alignment at 0/5/50/95/100%, at 100% and 300% scale, including shared rounded clipping, out-of-range dragging and tooltip dismissal. Inspected the corrected 300% near-end preview. Other theme/title/settings checks passed. Release build passed with 0 warnings/errors; diff whitespace checks passed. Live pointer/dragging acceptance remains pending.
- **User acceptance:** Pending.
- **Commit:** `0f63a00` - Standardize player themes and settings; refine Square progress UI.
