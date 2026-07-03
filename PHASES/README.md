# Phases

This folder contains the numbered phase plans for the project.

## Rules
- Work phases in order.
- Do not start the next phase until the current phase is complete.
- Keep every phase document detailed enough to drive implementation and review.
- Use `MODE: DEEP_WORK` for implementation planning and execution.
- Use `MODE: CODE_REVIEW` when reviewing code or changes.

## Phase File Format
Each numbered phase file should include:
- Scope and goal
- Features to add
- Improvements to make
- Things to avoid
- Verification and acceptance checks
- Related files or pages
- Logging or review notes

## Task Folder Format
If a phase needs finer granularity, place task documents in a matching subfolder named after the phase file stem.
Example: Phase 02 uses [02-foundation](02-foundation/README.md) and numbered task docs inside that folder.

## Current Phases
- [01-discovery-and-standards.md](01-discovery-and-standards.md)
- [02-foundation.md](02-foundation.md)
- [03-native-media-integration.md](03-native-media-integration.md)
- [04-ui-rendering.md](04-ui-rendering.md)
- [05-lifecycle-and-automation.md](05-lifecycle-and-automation.md)
- [06-hardening-and-release-readiness.md](06-hardening-and-release-readiness.md)