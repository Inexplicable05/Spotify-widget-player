"""Logging configuration for the Capsule Player backend."""

from __future__ import annotations

import logging
from logging.handlers import RotatingFileHandler
from pathlib import Path


def configure_logging(level_name: str, log_file_path: Path | None = None) -> None:
    """Configure process logging with a consistent format."""

    level = getattr(logging, level_name.upper(), logging.INFO)
    handlers: list[logging.Handler] = [logging.StreamHandler()]
    if log_file_path is not None:
        log_file_path.parent.mkdir(parents=True, exist_ok=True)
        handlers.append(
            RotatingFileHandler(log_file_path, maxBytes=1_048_576, backupCount=3, encoding="utf-8")
        )

    logging.basicConfig(
        level=level,
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
        handlers=handlers,
        force=True,
    )
    logging.captureWarnings(True)