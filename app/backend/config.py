"""Runtime configuration for the Capsule Player backend."""

from __future__ import annotations

import os
from dataclasses import dataclass
from pathlib import Path

from app.backend.errors import BackendConfigError


@dataclass(frozen=True, slots=True)
class BackendSettings:
    """Backend settings loaded from the environment."""

    app_name: str
    log_level: str
    frontend_path: Path
    log_file_path: Path | None


def _read_env(name: str, default: str | None = None) -> str | None:
    value = os.environ.get(name)
    if value is None:
        return default
    stripped = value.strip()
    return stripped if stripped else default


def load_backend_settings() -> BackendSettings:
    """Load backend settings from environment variables."""

    project_root = Path(__file__).resolve().parents[2]
    frontend_path_value = _read_env(
        "CAPSULE_PLAYER_FRONTEND_PATH",
        str(project_root / "app" / "frontend" / "index.html"),
    )
    assert frontend_path_value is not None
    frontend_path = Path(frontend_path_value).expanduser().resolve()
    if not frontend_path.exists():
        raise BackendConfigError(f"Frontend entry point does not exist: {frontend_path}")

    log_file_value = _read_env("CAPSULE_PLAYER_LOG_FILE")
    log_file_path = Path(log_file_value).expanduser().resolve() if log_file_value else None

    return BackendSettings(
        app_name=_read_env("CAPSULE_PLAYER_APP_NAME", "Capsule Player") or "Capsule Player",
        log_level=_read_env("CAPSULE_PLAYER_LOG_LEVEL", "INFO") or "INFO",
        frontend_path=frontend_path,
        log_file_path=log_file_path,
    )