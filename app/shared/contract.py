"""Shared frontend/backend contract for the Capsule Player."""

from __future__ import annotations

from dataclasses import dataclass
from enum import StrEnum


class MediaCommand(StrEnum):
    """Supported user-initiated media actions."""

    PREVIOUS = "prev"
    PLAY_PAUSE = "playpause"
    NEXT = "next"


@dataclass(frozen=True, slots=True)
class TrackMetadata:
    """Track metadata shared across the UI boundary."""

    title: str | None
    artist: str | None
    album_art_data_uri: str | None

    @classmethod
    def empty(cls) -> "TrackMetadata":
        return cls(title=None, artist=None, album_art_data_uri=None)

    def to_dict(self) -> dict[str, str | None]:
        return {
            "title": self.title,
            "artist": self.artist,
            "album_art_data_uri": self.album_art_data_uri,
        }


@dataclass(frozen=True, slots=True)
class PlaybackState:
    """Normalized playback state shared across the application boundary."""

    is_playing: bool
    track: TrackMetadata | None
    position_ms: int | None
    duration_ms: int | None

    @classmethod
    def empty(cls) -> "PlaybackState":
        return cls(is_playing=False, track=None, position_ms=None, duration_ms=None)

    def to_dict(self) -> dict[str, object | None]:
        return {
            "is_playing": self.is_playing,
            "track": None if self.track is None else self.track.to_dict(),
            "position_ms": self.position_ms,
            "duration_ms": self.duration_ms,
        }


@dataclass(frozen=True, slots=True)
class CommandResult:
    """Structured response for backend command handling."""

    command: str
    accepted: bool
    message: str

    def to_dict(self) -> dict[str, str | bool]:
        return {
            "command": self.command,
            "accepted": self.accepted,
            "message": self.message,
        }


@dataclass(frozen=True, slots=True)
class UiState:
    """State snapshot rendered by the frontend shell."""

    ready: bool
    status_message: str
    playback_state: PlaybackState
    last_command: CommandResult | None = None

    @classmethod
    def empty(cls, status_message: str = "To be built") -> "UiState":
        return cls(
            ready=False,
            status_message=status_message,
            playback_state=PlaybackState.empty(),
            last_command=None,
        )

    def to_dict(self) -> dict[str, object | None]:
        return {
            "ready": self.ready,
            "status_message": self.status_message,
            "playback_state": self.playback_state.to_dict(),
            "last_command": None if self.last_command is None else self.last_command.to_dict(),
        }