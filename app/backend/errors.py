"""Backend exceptions for the Capsule Player application."""

from __future__ import annotations


class BackendConfigError(RuntimeError):
    """Raised when runtime configuration is missing or invalid."""