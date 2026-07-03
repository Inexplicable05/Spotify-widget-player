# Phase 03 Task 01 - Media Gateway

## Goal
Create a single backend gateway that owns native media access and keeps platform-specific code out of the API surface.

## What Will Be Built
- A backend media gateway class.
- A clear method for reading current media state.
- A clear method for sending transport commands.
- Logging around gateway availability and failures.

## Features to Add
- Media gateway module.
- Gateway methods for state and commands.
- Availability checks for the native media session.
- Explicit fallback responses when the session is not ready.

## Improvements to Make
- Centralize all OS media access in one place.
- Avoid exposing platform code to the frontend bridge.
- Make gateway behavior predictable for later tasks.

## Things to Avoid
- Do not put Windows API calls in the frontend.
- Do not spread media access across multiple backend modules.
- Do not fake media state for gateway tests.

## How to Check This Task
- Confirm the gateway module is the only backend entry point for native media.
- Confirm it returns explicit unavailable state when the bridge is not yet implemented.
- Confirm logs explain whether the gateway is available.

## Acceptance Criteria
- There is one backend gateway for native media access.
- The gateway has clear state and command methods.
- The gateway can be replaced with the real Windows adapter later without changing the API contract.