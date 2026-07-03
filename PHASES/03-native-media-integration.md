# Phase 03 - Native Media Integration

## Goal

Connect the widget to Windows media state and playback controls through the native operating-system path.

## What Will Be Built

- A backend gateway for native media commands and state retrieval.
- A Windows media session adapter boundary.
- Explicit unavailable and empty states for when Spotify or the media session is not ready.
- Track metadata and playback normalization before data reaches the UI.
- A native-media verification workflow for command and state behavior.

## Task Docs

- [03-native-media-integration/README.md](03-native-media-integration/README.md)
- [03-native-media-integration/01-media-gateway.md](03-native-media-integration/01-media-gateway.md)
- [03-native-media-integration/02-command-routing.md](03-native-media-integration/02-command-routing.md)
- [03-native-media-integration/03-metadata-and-artwork.md](03-native-media-integration/03-metadata-and-artwork.md)
- [03-native-media-integration/04-empty-states-and-fallbacks.md](03-native-media-integration/04-empty-states-and-fallbacks.md)
- [03-native-media-integration/05-verification-and-quality.md](03-native-media-integration/05-verification-and-quality.md)

## Features to Add

- Play, pause, next, and previous commands through native media control.
- Track metadata retrieval from the OS media session.
- Playback state retrieval from the OS media session.
- Album art retrieval from the live media source.
- Explicit empty-state handling when Spotify or media data is unavailable.

## Improvements to Make

- Make media access resilient to session changes and missing permissions.
- Reduce unnecessary polling and only refresh when needed.
- Normalize data before it reaches the UI layer.
- Keep transport logic isolated from presentation logic.

## Things to Avoid

- Do not fabricate titles, artists, album art, or progress values.
- Do not use network APIs when the local OS source is the intended source of truth.
- Do not let UI code talk directly to platform APIs.
- Do not hide missing state behind placeholder content.

## How to Check This Phase

- Trigger each playback command and confirm the native media state changes.
- Change tracks in Spotify and confirm metadata updates without stale values.
- Stop playback or remove the source and confirm the UI shows an empty state.
- Verify there is a documented fallback for unavailable media sessions.

## Acceptance Criteria

- Playback commands work from the widget.
- Live media metadata is reflected accurately.
- Missing data is handled explicitly and honestly.

## Related Files

- [architecture.md](../architecture.md)
- [UiSample.html](../UiSample.html)

## Notes

- This phase is complete only when the widget is driven by real OS media state, not mocked content.
