"""Executable quality gate for Phase 02 foundation work."""

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

    backend_main = importlib.import_module("app.backend.main")
    backend_api = importlib.import_module("app.backend.api")
    shared_contract = importlib.import_module("app.shared.contract")

    require(backend_main.main() == 0, "Backend bootstrap should succeed.", failures)

    api = backend_api.CapsulePlayerApi()
    state = api.get_ui_state()
    require(state["ready"] is False, "UI state should report not ready.", failures)
    require(
        "to be built" in str(state["status_message"]).lower(),
        "UI state should explain that the media bridge is not built yet.",
        failures,
    )

    valid_command = api.media_command(shared_contract.MediaCommand.PLAY_PAUSE.value)
    require(valid_command["accepted"] is False, "Media commands should not claim success yet.", failures)
    require(
        "to be built" in str(valid_command["message"]).lower(),
        "Media command response should be explicit about the unbuilt bridge.",
        failures,
    )

    frontend_index = (ROOT / "app" / "frontend" / "index.html").read_text(encoding="utf-8")
    require("To be built" in frontend_index, "Frontend should show an empty-state message.", failures)
    require(
        "window.pywebview.api.media_command" not in frontend_index,
        "Frontend contract should live in app.js, not the HTML shell.",
        failures,
    )

    frontend_script = (ROOT / "app" / "frontend" / "app.js").read_text(encoding="utf-8")
    require("media_command" in frontend_script, "Frontend script should call the backend command contract.", failures)
    require("get_ui_state" in frontend_script, "Frontend script should request the backend UI state.", failures)

    if failures:
        for failure in failures:
            print(f"FAIL: {failure}")
        return 1

    print("Phase 02 quality gates passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())