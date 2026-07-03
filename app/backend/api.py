"""Backend API surface exposed to the frontend shell."""

from __future__ import annotations

import logging

from app.backend.media import MediaGateway
from app.shared.contract import CommandResult, MediaCommand, UiState


class CapsulePlayerApi:
    """Bridge object exposed to the webview frontend."""

    def __init__(self) -> None:
        self._logger = logging.getLogger(self.__class__.__name__)
        self._media_gateway = MediaGateway()

    def media_command(self, command: str) -> dict[str, object | None]:
        """Accept a media command from the frontend.
        """

        try:
            normalized = MediaCommand(command)
        except ValueError:
            self._logger.warning("Received unknown media command: %s", command)
            return CommandResult(
                command=command,
                accepted=False,
                message=f"Unknown media command: {command}",
            ).to_dict()

        self._logger.info("Received media command: %s", normalized.value)
        return self._media_gateway.send_command(normalized.value).to_dict()

    def seek_position(self, position_ms: int) -> dict[str, object | None]:
        """Seek the active media session to a playback position."""

        self._logger.info("Received media seek request: %s ms", position_ms)
        return self._media_gateway.seek_position(position_ms).to_dict()

    def get_ui_state(self) -> dict[str, object | None]:
        """Return the current UI state snapshot."""

        self._logger.debug("Returning media-backed UI state snapshot")
        return self._media_gateway.get_ui_state().to_dict()