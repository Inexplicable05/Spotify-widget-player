# Phase 02 Task 05 - Testing and Quality Gates

## Goal
Define the minimum test and quality structure that will protect the foundation from regressions.

## What Will Be Built
- A quality gate checklist for foundation changes.
- A test plan for structure, config, logging, and contract behavior.
- Lint, format, and static-check expectations.
- A phase completion checklist.

## Built Artifacts
- [tools/verify_phase_02.py](../../tools/verify_phase_02.py)
- [app/backend/api.py](../../app/backend/api.py)
- [app/shared/contract.py](../../app/shared/contract.py)
- [app/frontend/contract.js](../../app/frontend/contract.js)
- [app/frontend/app.js](../../app/frontend/app.js)

## Features to Add
- Smoke checks for startup path assumptions.
- Validation for missing config behavior.
- Validation for error and empty-state behavior.
- Documentation of what must pass before moving forward.

## Improvements to Make
- Make the phase executable, not just descriptive.
- Reduce ambiguity about what counts as done.
- Catch structural problems before feature work starts.

## Things to Avoid
- Do not mark the phase complete without running checks.
- Do not postpone tests until after later feature phases.
- Do not accept unverified assumptions as pass criteria.

## How to Check This Task
- Confirm the checklist covers each foundation concern.
- Confirm the tests or checks are tied to the actual files and behavior.
- Confirm failures block progression to the next phase.

## Verification Steps
- Run `python tools/verify_phase_02.py` from the project root.
- Confirm the backend bootstrap still returns success.
- Confirm the frontend shell uses the explicit empty state instead of sample data.
- Confirm the backend command response says the bridge is still to be built rather than pretending a media action succeeded.

## Acceptance Criteria
- Quality gates are defined before feature work begins.
- The foundation phase has a measurable exit condition.
- Reviewers can determine whether the phase is complete.

## Notes
- This task is complete only when the phase can be verified instead of assumed.