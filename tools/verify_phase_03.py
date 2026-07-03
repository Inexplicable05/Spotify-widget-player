"""Executable quality gate for Phase 03 native media integration work."""

from __future__ import annotations

import importlib
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]


def require(condition: bool, message: str, failures: list[str]) -> None:
    if not condition:
        failures.append(message)


def main() -> int:
    sys.path.insert(0, str(ROOT))
    failures: list[str] = []

    backend_api = importlib.import_module("app.backend.api")
    backend_media = importlib.import_module("app.backend.media")

    gateway = backend_media.MediaGateway()
    state = gateway.get_ui_state().to_dict()
    if state["ready"]:
        require(
            "active" in str(state["status_message"]).lower(),
            "Active native media state should explain that integration is active.",
            failures,
        )
        track = state["playback_state"]["track"]
        require(track is not None, "Active native media state should include track metadata.", failures)
        if track is not None:
            require(bool(track["title"]), "Active native media state should include a track title.", failures)
            require(bool(track["artist"]), "Active native media state should include a track artist.", failures)
    else:
        require(
            "unavailable" in str(state["status_message"]).lower(),
            "Unavailable native media state should explain why it is unavailable.",
            failures,
        )
        require(
            state["playback_state"]["track"] is None,
            "Unavailable native media state should not invent track metadata.",
            failures,
        )

    invalid_command = gateway.send_command("invalid").to_dict()
    require(invalid_command["accepted"] is False, "Invalid native media commands should be rejected.", failures)
    require(
        "unknown" in str(invalid_command["message"]).lower(),
        "Invalid command responses should be explicit about the rejection.",
        failures,
    )

    if state["ready"]:
        supported_command_shape = gateway.send_command("playpause").to_dict()
        require(
            supported_command_shape["command"] == "playpause",
            "Supported command responses should preserve the command name.",
            failures,
        )
        require(
            isinstance(supported_command_shape["accepted"], bool),
            "Supported command responses should include a boolean acceptance flag.",
            failures,
        )

    api = backend_api.CapsulePlayerApi()
    api_state = api.get_ui_state()
    require(api_state["ready"] == state["ready"], "API state should mirror the native media gateway readiness.", failures)
    require(
        api_state["status_message"] == state["status_message"],
        "API state should mirror the native media gateway status message.",
        failures,
    )

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    print("Phase 03 quality gates passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())