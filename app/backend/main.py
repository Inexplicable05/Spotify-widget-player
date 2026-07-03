"""Backend entry point for the Capsule Player desktop widget."""

from __future__ import annotations

import logging

from app.backend.config import load_backend_settings
from app.backend.errors import BackendConfigError
from app.backend.diagnostics import (
    log_expected_failure,
    log_ready,
    log_shutdown,
    log_startup,
    log_unexpected_failure,
)
from app.backend.launcher import LauncherConfig, launch_desktop_widget
from app.backend.logging_setup import configure_logging


def main() -> int:
    """Start the backend process."""

    logger = logging.getLogger(__name__)
    try:
        settings = load_backend_settings()
        configure_logging(settings.log_level, settings.log_file_path)
        log_startup(logger, settings.app_name, str(settings.frontend_path))
        log_ready(logger)
        launch_desktop_widget(
            LauncherConfig(
                title=settings.app_name,
                frontend_path=settings.frontend_path,
            )
        )
        log_shutdown(logger)
        return 0
    except BackendConfigError:
        configure_logging("INFO")
        log_expected_failure(logger, "Backend configuration is invalid")
        return 1
    except Exception:
        configure_logging("INFO")
        log_unexpected_failure(logger, "Unhandled backend failure")
        return 1


if __name__ == "__main__":
    raise SystemExit(main())