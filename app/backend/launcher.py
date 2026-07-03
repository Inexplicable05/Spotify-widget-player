"""Desktop window launcher for the Capsule Player widget."""

from __future__ import annotations

from dataclasses import dataclass
from pathlib import Path

import webview

from app.backend.api import CapsulePlayerApi


@dataclass(frozen=True, slots=True)
class LauncherConfig:
    """Runtime configuration for the widget window."""

    title: str
    frontend_path: Path


def launch_desktop_widget(config: LauncherConfig) -> None:
    """Open the desktop widget window and hand control to pywebview."""

    webview.create_window(
        title=config.title,
        url=config.frontend_path.as_uri(),
        js_api=CapsulePlayerApi(),
        frameless=True,
        transparent=True,
        easy_drag=False,
        shadow=False,
        background_color="#000000",
        width=720,
        height=144,
        resizable=False,
        on_top=True,
    )
    webview.start(debug=False, gui="edgechromium")