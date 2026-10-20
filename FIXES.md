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
- **Commit:** None.

### P002 - Unsupported media sessions take over playback metadata and controls

- **Status:** Open - awaiting solution approval. Not fixed.
- **Reported:** Netflix and other unwanted media appear in the widget. Allow Spotify, Apple Music, Amazon Music, and YouTube / YouTube Music in the browser; ignore other services.
- **Cause:** `UpdateCurrentSession` calls Windows `GetCurrentSession` without filtering. Visibility separately depends only on the Spotify process, so other supported players cannot independently make the widget visible.
- **Proposed approach:** Enumerate sessions and allow verified desktop app identities, retain an eligible session while it remains active, and use the selected eligible session for visibility and all shared metadata/commands. Browser app identity alone cannot distinguish YouTube from Netflix. Reliable browser service filtering requires a trusted browser integration that identifies the actual media tab URL, rather than guessing from titles or allowing the entire browser.
- **References:** [Windows session API](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssession), [Windows media properties](https://learn.microsoft.com/en-us/uwp/api/windows.media.control.globalsystemmediatransportcontrolssessionmediaproperties), [Chrome tabs API](https://developer.chrome.com/docs/extensions/reference/api/tabs).
- **Verification:** Source inspection and API documentation. Runtime source identities and browser integration not yet verified.
- **User acceptance:** Pending.
- **Commit:** None.

### P003 - Landscape thumbnail leaves exposed artwork corners in Disk layout

- **Status:** Open - source inspection consistent with screenshot; awaiting solution approval. Not fixed.
- **Reported:** YouTube thumbnail appears as a misaligned rotating rectangle inside the player. Screenshot shows Disk layout; user also mentioned Square, which must be checked with landscape artwork.
- **Cause:** Disk rotates the `Image` directly, while only the outer `DiscGrid` applies a circular clip. The image has no explicit square viewport or crop before rotation. Rectangular artwork/layout clipping can rotate through the circle instead of presenting a fully covered disc. Shared artwork assignment accepts the provided thumbnail unchanged. A video thumbnail is the artwork supplied by the media session, not necessarily a separate album cover.
- **Proposed approach:** Center-crop artwork into a fixed square viewport before rotating that viewport, retain the outer circular clip, and apply the same explicit fill/crop bounds to Square artwork and its hover blur. Preserve aspect ratio; do not stretch thumbnails. Confirm with landscape, portrait, and square artwork at several rotation angles.
- **References:** [WPF imaging and UniformToFill](https://learn.microsoft.com/en-us/dotnet/framework/wpf/graphics-multimedia/imaging-overview), [WPF transforms](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/transforms-overview).
- **Verification:** Source inspection only; reproduce with rectangular test images before implementation.
- **User acceptance:** Pending.
- **Commit:** None.
