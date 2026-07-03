"""Native media gateway boundary for the Capsule Player backend."""

from __future__ import annotations

import logging

from app.backend.native_media import create_native_media_adapter
from app.shared.contract import CommandResult, MediaCommand, PlaybackState, TrackMetadata, UiState


class MediaGateway:
    """Owns native media state and command access behind a single boundary."""

    def __init__(self) -> None:
        self._logger = logging.getLogger(self.__class__.__name__)
        self._adapter = create_native_media_adapter()

    def get_playback_state(self) -> PlaybackState:
        """Return the current playback state.

        The real Windows media integration is not wired yet, so this returns
        an explicit empty state instead of fabricated track data.
        """

        status = self._adapter.is_available()
        self._logger.debug("Media gateway playback state requested: %s", status.reason)
        return self._adapter.get_playback_state()

    def get_ui_state(self) -> UiState:
        """Return the UI snapshot backed by native media state."""

        status = self._adapter.is_available()
        if status.available:
            playback_state = self._adapter.get_playback_state()
            if playback_state.track is None:
                track = self._adapter.describe_track()
                if track.title is not None:
                    playback_state = PlaybackState(
                        is_playing=playback_state.is_playing,
                        track=track,
                        position_ms=playback_state.position_ms,
                        duration_ms=playback_state.duration_ms,
                    )
            return UiState(ready=True, status_message="Native media integration is active.", playback_state=playback_state)

        self._logger.debug("Media gateway unavailable: %s", status.reason)
        return UiState.empty(status_message=f"Native media integration unavailable: {status.reason}")

    def send_command(self, command: str) -> CommandResult:
        """Route a media command through the native media boundary."""

        try:
            normalized = MediaCommand(command)
        except ValueError:
            self._logger.warning("Rejected unsupported native media command: %s", command)
            return CommandResult(command=command, accepted=False, message=f"Unknown media command: {command}")

        status = self._adapter.is_available()
        if not status.available:
            self._logger.info("Native media command %s unavailable: %s", normalized.value, status.reason)
            return CommandResult(
                command=normalized.value,
                accepted=False,
                message=f"Native media integration unavailable: {status.reason}",
            )

        self._logger.info("Accepted native media command: %s", normalized.value)
        message = self._adapter.send_command(normalized.value)
        return CommandResult(command=normalized.value, accepted=True, message=message)

    def describe_track(self) -> TrackMetadata:
        """Return normalized track metadata for the current session."""

        status = self._adapter.is_available()
        self._logger.debug("Media gateway requested track metadata: %s", status.reason)
        return self._adapter.describe_track()

    def seek_position(self, position_ms: int) -> CommandResult:
        """Seek the active media session to the requested position."""

        status = self._adapter.is_available()
        if not status.available:
            self._logger.info("Native media seek unavailable: %s", status.reason)
            return CommandResult(command="seek", accepted=False, message=f"Native media integration unavailable: {status.reason}")

        clamped_position_ms = max(0, int(position_ms))
        self._logger.info("Accepted native media seek: %s ms", clamped_position_ms)
        message = self._adapter.seek_position(clamped_position_ms)
        return CommandResult(command="seek", accepted=True, message=message)