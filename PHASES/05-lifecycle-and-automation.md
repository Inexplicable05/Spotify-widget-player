# Phase 05 - Lifecycle and Automation

## Goal
Add startup, shutdown, and process-driven lifecycle handling for the widget.

## Features to Add
- Startup handling for the background process.
- Process detection for Spotify availability.
- Automatic widget creation when the target process appears.
- Automatic widget teardown when the target process disappears.
- Safe recovery paths for denied access or missing dependencies.

## Improvements to Make
- Make lifecycle behavior observable through logs.
- Avoid unnecessary polling and resource waste.
- Keep the process manager separate from the UI logic.
- Ensure failures are recoverable without manual cleanup whenever possible.

## Things to Avoid
- Do not leave orphaned windows or background loops running.
- Do not guess process state when the OS can confirm it.
- Do not silently swallow lifecycle failures.
- Do not tie automation to UI assumptions that may change.

## How to Check This Phase
- Start and stop Spotify and confirm widget lifecycle behavior matches the design.
- Confirm the background process can recover from a missing target process.
- Confirm logs explain what happened when lifecycle behavior changes.
- Confirm resource usage remains lightweight.

## Acceptance Criteria
- The widget lifecycle follows the intended process rules.
- Recovery behavior is documented and testable.
- The automation path does not create hidden failure states.

## Related Files
- [PROJECT_LOG.md](../PROJECT_LOG.md)

## Notes
- This phase is complete only when lifecycle automation is reliable and explainable.