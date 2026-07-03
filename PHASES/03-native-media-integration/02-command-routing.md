# Phase 03 Task 02 - Command Routing

## Goal
Route play, pause, next, and previous commands through the backend gateway to the native media layer.

## What Will Be Built
- Command normalization.
- Command routing from the API to the media gateway.
- Structured command success or failure responses.

## Features to Add
- Native command adapter hook.
- Consistent handling for unsupported or invalid commands.
- Logging for command attempts and failures.

## Improvements to Make
- Keep command handling narrow and explicit.
- Make command responses easy for the UI to consume.
- Prevent the frontend from guessing the effect of a command.

## Things to Avoid
- Do not let unsupported commands fail silently.
- Do not return success when the command was not actually sent.
- Do not build command logic in the frontend.

## How to Check This Task
- Trigger each command path and confirm the backend response is structured.
- Confirm invalid commands are rejected clearly.
- Confirm the command path is still separated from UI rendering.

## Acceptance Criteria
- Commands flow through the backend gateway.
- Responses explain whether a command was accepted or rejected.
- The UI can show an honest status message.