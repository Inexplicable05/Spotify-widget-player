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
