# Capsule Player

Desktop widget for Spotify playback control and media state visualization.

## Current Build State
- Phase 02 foundation is in progress.
- The initial application skeleton is in place under `app/`.
- Later tasks will fill in configuration, logging, contract, and feature behavior.
- The desktop launcher is wired through `app/backend/main.py` and opens the pywebview widget host.

## How To Start
- Run `python -m app.backend.main` from the project venv.
- The backend opens the frameless transparent widget window using the local HTML frontend.
- The UI talks to the backend through the pywebview API bridge.

## Structure
- `app/backend/` backend entry point and backend support code.
- `app/frontend/` frontend shell for the widget UI.
- `app/shared/` shared types and contracts.
- `PHASES/` phase planning and task documents.

## Rules
- Do not use fake media data.
- Show empty states when data is unavailable.
- Build one phase at a time.

## Environment Variables
- `CAPSULE_PLAYER_APP_NAME` sets the application name shown in logs.
- `CAPSULE_PLAYER_LOG_LEVEL` sets the backend log level.
- `CAPSULE_PLAYER_FRONTEND_PATH` overrides the frontend entry point.
- `CAPSULE_PLAYER_LOG_FILE` enables file logging when set.