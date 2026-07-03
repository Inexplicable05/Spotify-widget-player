# Phase 03 Task 04 - Empty States and Fallbacks

## Goal
Define the honest fallback behavior for missing sessions, unavailable playback state, and denied access.

## What Will Be Built
- Empty-state messages for no active media session.
- Fallback behavior when Spotify is not running.
- Fallback behavior when metadata is incomplete.

## Features to Add
- Clear unavailable-state handling.
- Status messages that explain the absence of media data.
- Recovery cues for later automation work.

## Improvements to Make
- Make error states understandable to the user.
- Prevent stale state from lingering in the UI.
- Keep fallback messaging consistent across the app.

## Things to Avoid
- Do not masquerade stale data as fresh data.
- Do not hide failure states behind generic success text.
- Do not make the fallback path depend on fake payloads.

## How to Check This Task
- Remove or stop the media source and confirm the UI shows an empty state.
- Confirm the backend response explains why data is unavailable.
- Confirm fallbacks are still compatible with future real data.

## Acceptance Criteria
- Missing media is represented honestly.
- Fallback states are explicit and useful.
- The UI and backend agree on empty-state behavior.