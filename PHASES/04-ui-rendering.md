# Phase 04 - UI Rendering

## Goal
Render the widget shell so it reflects live media state with a polished, production-ready interface.

## Features to Add
- Album art rendering from real data.
- Track title and artist rendering from real data.
- Playback progress rendering from real state.
- Loading, empty, and unavailable states.
- Clean interaction states for buttons, sliders, and window dragging.

## Improvements to Make
- Ensure the visual design remains cohesive under real-world data.
- Improve accessibility for keyboard and screen-reader use.
- Keep the UI responsive and stable at different window sizes.
- Make the state transitions feel deliberate, not flashy.

## Things to Avoid
- Do not use sample artwork or invented track names.
- Do not make the interface depend on perfect data being present.
- Do not sacrifice readability for visual effects.
- Do not create UI states that cannot be reproduced by the backend.

## How to Check This Phase
- Verify the UI shows a clear message when data is missing.
- Verify controls remain usable when the window is draggable.
- Verify progress, artwork, and metadata update from live backend input.
- Verify empty states do not look like errors unless they are errors.

## Acceptance Criteria
- The widget is visually complete without fake data.
- UI states are clear and accurate.
- The interface is ready for integration testing.

## Related Files
- [UiSample.html](../UiSample.html)

## Notes
- This phase is complete only when the UI reflects real system state and handles absence cleanly.