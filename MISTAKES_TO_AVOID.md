# Mistakes to Avoid

## Process Mistakes
- Do not skip phases or jump into implementation before standards are settled.
- Do not mix planning, review, and implementation in the same step without a clear reason.
- Do not move to a later phase before the current one is complete.
- Do not keep phase planning in a flat root file when the agreed structure is a folder of numbered phase documents.
- Do not leave a phase at a high level when the build needs task-level docs to be executable.

## Build Mistakes
- Do not blur backend, frontend, and shared boundaries in the application layout.
- Do not turn scaffold files into feature logic before the contract and configuration tasks are done.
- Do not silently accept a missing frontend entry point or invalid runtime settings.
- Do not let the frontend imply a media bridge is working when it is still to be built.
- Do not catch failures without logging enough context to diagnose the cause later.
- Do not spread native media session code outside the gateway boundary.
- Do not pretend `winsdk` is installed or buildable when the toolchain required by the package is missing.
- Do not claim native media integration is complete when the current environment can only exercise the fallback path.
- Do not keep troubleshooting `winsdk` on Python 3.14 when the project already needs a supported older interpreter.

## Data and UI Mistakes
- Do not invent track, album, or playback data to make the UI look complete.
- Do not use dummy artwork or fake playback progress for unfinished features.
- Do not hide missing data; show a clear empty state or "to be built" message instead.

## Quality Mistakes
- Do not accept ad hoc code that bypasses the agreed architecture.
- Do not introduce unlogged deviations from the standard approach.
- Do not treat placeholder logic as production behavior.