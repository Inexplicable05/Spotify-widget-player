# Phase 01 - Discovery and Standards

## Goal
Confirm the product direction, implementation standards, and operating rules before any application code is written.

## Features to Add
- A written architecture contract for the widget.
- A locked development mode policy for planning, review, and implementation.
- A shared rule set for how missing data is handled.
- A phase-by-phase workflow that prevents skipping ahead.

## Improvements to Make
- Clarify the minimum viable product so scope does not drift.
- Resolve the canonical source of truth for playback state and metadata.
- Define logging expectations for future code and process changes.
- Define quality gates before implementation starts.

## Things to Avoid
- Do not introduce placeholder data, fake artwork, or mock playback values.
- Do not write feature code before standards and dependencies are agreed.
- Do not blend phase planning with implementation work.
- Do not move to a later phase without closing this one.

## How to Check This Phase
- Confirm the architecture document matches the intended product direction.
- Confirm the process docs exist and are readable from the repo root.
- Confirm the no-dummy-data rule is written in the mistakes log.
- Confirm the project log captures the current phase and next actions.

## Acceptance Criteria
- The scope is stable and documented.
- The process rules are written and versioned.
- There is a clear path to start the foundation phase.

## Related Files
- [architecture.md](../architecture.md)
- [PROJECT_LOG.md](../PROJECT_LOG.md)
- [MISTAKES_TO_AVOID.md](../MISTAKES_TO_AVOID.md)

## Notes
- This phase is complete only when the process rules are explicit enough that implementation can proceed without ambiguity.