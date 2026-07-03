# Phase 03 Task 03 - Metadata and Artwork

## Goal
Normalize track metadata and album art from the native media source before handing it to the UI.

## What Will Be Built
- Metadata normalization for title and artist.
- Album art normalization into a UI-safe representation.
- A stable data shape for track information.

## Features to Add
- Track metadata mapping.
- Artwork data preparation.
- A null-safe shape for missing fields.

## Improvements to Make
- Make the UI data predictable.
- Keep media-specific logic outside presentation code.
- Make it obvious when artwork or metadata is unavailable.

## Things to Avoid
- Do not use fallback images that pretend to be the current track.
- Do not invent track names or artists.
- Do not let one missing field break the rest of the payload.

## How to Check This Task
- Confirm metadata is normalized into the shared contract shape.
- Confirm missing artwork yields an explicit empty state.
- Confirm the UI can render partial data without crashing.

## Acceptance Criteria
- Metadata and artwork are normalized before reaching the frontend.
- Missing fields are represented honestly.
- The payload remains stable for the UI.