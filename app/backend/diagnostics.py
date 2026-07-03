"""Diagnostics helpers for backend startup, shutdown, and failure reporting."""

from __future__ import annotations

import logging


def log_startup(logger: logging.Logger, app_name: str, frontend_path: str) -> None:
    logger.info("Starting %s backend shell", app_name)
    logger.info("Frontend entry point: %s", frontend_path)


def log_ready(logger: logging.Logger) -> None:
    logger.info("Backend bootstrap complete; runtime wiring is still to be built.")


def log_shutdown(logger: logging.Logger) -> None:
    logger.info("Backend shutdown requested")


def log_expected_failure(logger: logging.Logger, message: str) -> None:
    logger.error(message)


def log_unexpected_failure(logger: logging.Logger, message: str) -> None:
    logger.exception(message)