# Phase 02 Task 04 - Frontend and Backend Contract

## Goal
Define the data and action contract between the UI and the backend before feature implementation begins.

## What Will Be Built
- A command contract for media actions.
- A state contract for metadata, playback, and availability.
- A clear shape for empty and loading states.
- A documented path for UI events to reach the backend.
- A documented path for backend state to reach the UI.

## Features to Add
- Typed or explicitly structured payloads.
- Contract definitions for track metadata and playback state.
- Empty-state signaling for missing media or unavailable sessions.
- Command responses that confirm success or explain failure.

## Improvements to Make
- Prevent the UI from guessing backend state.
- Prevent the backend from sending ambiguous payloads.
- Make contract changes easy to review in isolation.

## Things to Avoid
- Do not rely on ad hoc object shapes.
- Do not send fake data to make the interface look populated.
- Do not let one side of the app assume fields that are not guaranteed.

## How to Check This Task
- Confirm each UI action has a matching backend command.
- Confirm each backend state update has a defined UI consumer.
- Confirm empty and unavailable states are explicitly represented.

## Acceptance Criteria
- The backend and frontend agree on a documented contract.
- Missing data is represented honestly.
- Future features can use the same contract without redesign.

## Notes
- This task is complete only when UI and backend can communicate without guesswork.