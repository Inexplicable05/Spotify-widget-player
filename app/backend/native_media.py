"""Native media adapter boundary for Windows-backed playback integration."""

from __future__ import annotations

import asyncio
import base64
import threading
from datetime import timedelta
from dataclasses import dataclass
from typing import Protocol

from winsdk.windows.media.control import (
    GlobalSystemMediaTransportControlsSession,
    GlobalSystemMediaTransportControlsSessionManager,
    GlobalSystemMediaTransportControlsSessionPlaybackStatus,
)
from winsdk.windows.storage.streams import Buffer, DataReader

from app.shared.contract import PlaybackState, TrackMetadata


@dataclass(frozen=True, slots=True)
class NativeMediaStatus:
    """Availability and failure details for the native media adapter."""

    available: bool
    reason: str


class NativeMediaAdapter(Protocol):
    """Interface for a platform-specific media adapter."""

    def is_available(self) -> NativeMediaStatus:
        """Report whether the adapter can talk to the native media session."""

    def get_playback_state(self) -> PlaybackState:
        """Return the current playback state."""

    def send_command(self, command: str) -> str:
        """Send a transport command and return a human-readable outcome."""

    def describe_track(self) -> TrackMetadata:
        """Return normalized track metadata."""

    def seek_position(self, position_ms: int) -> str:
        """Seek the active session to a playback position in milliseconds."""


def _run_async(awaitable):
    async def _runner():
        return await awaitable

    return asyncio.run(_runner())


def _stream_to_data_uri(stream) -> str:
    buffer = Buffer(stream.size)
    _run_async(stream.read_async(buffer, stream.size, 0))
    reader = DataReader.from_buffer(buffer)
    raw_bytes = bytearray(buffer.length)
    reader.read_bytes(raw_bytes)
    mime_type = getattr(stream, "content_type", None) or "image/png"
    encoded = base64.b64encode(bytes(raw_bytes)).decode("ascii")
    return f"data:{mime_type};base64,{encoded}"


def _is_playing(status) -> bool:
    return status == GlobalSystemMediaTransportControlsSessionPlaybackStatus.PLAYING


def _timedelta_to_milliseconds(value) -> int | None:
    if value is None:
        return None
    return int(value.total_seconds() * 1000)


def _milliseconds_to_ticks(value_ms: int) -> int:
    return int(value_ms) * 10_000


@dataclass(slots=True)
class UnavailableNativeMediaAdapter:
    """Adapter used when Windows media APIs are not available."""

    reason: str

    def is_available(self) -> NativeMediaStatus:
        return NativeMediaStatus(available=False, reason=self.reason)

    def get_playback_state(self) -> PlaybackState:
        return PlaybackState.empty()

    def send_command(self, command: str) -> str:
        return f"Native media integration is unavailable: {self.reason}"

    def describe_track(self) -> TrackMetadata:
        return TrackMetadata.empty()

    def seek_position(self, position_ms: int) -> str:
        return f"Native media integration is unavailable: {self.reason}"


@dataclass(slots=True)
class WinsdkNativeMediaAdapter:
    """Adapter backed by the Windows media session manager."""

    _manager: GlobalSystemMediaTransportControlsSessionManager
    _cache_lock: threading.Lock = threading.Lock()
    _cached_track_signature: str | None = None
    _cached_album_art_data_uri: str | None = None

    @classmethod
    def create(cls) -> "WinsdkNativeMediaAdapter":
        manager = _run_async(GlobalSystemMediaTransportControlsSessionManager.request_async())
        return cls(_manager=manager)

    def status(self) -> NativeMediaStatus:
        session = self._get_current_session()
        if session is None:
            return NativeMediaStatus(available=False, reason="No active media session is available")
        return NativeMediaStatus(available=True, reason="winsdk media session manager ready")

    def is_available(self) -> NativeMediaStatus:
        return self.status()

    def _get_current_session(self) -> GlobalSystemMediaTransportControlsSession | None:
        return self._manager.get_current_session()

    def _get_media_properties(self, session: GlobalSystemMediaTransportControlsSession):
        return _run_async(session.try_get_media_properties_async())

    def _read_track_metadata(self, session: GlobalSystemMediaTransportControlsSession) -> TrackMetadata:
        media = self._get_media_properties(session)
        title = getattr(media, "title", None) or None
        artist = getattr(media, "artist", None) or None
        signature = f"{title!r}:{artist!r}"

        with self._cache_lock:
            if self._cached_track_signature != signature:
                self._cached_track_signature = signature
                self._cached_album_art_data_uri = None

        album_art_data_uri = self._cached_album_art_data_uri
        if album_art_data_uri is None and getattr(media, "thumbnail", None) is not None:
            thumbnail_stream = _run_async(media.thumbnail.open_read_async())
            album_art_data_uri = _stream_to_data_uri(thumbnail_stream)
            with self._cache_lock:
                self._cached_album_art_data_uri = album_art_data_uri

        return TrackMetadata(
            title=title,
            artist=artist,
            album_art_data_uri=album_art_data_uri,
        )

    def get_playback_state(self) -> PlaybackState:
        session = self._get_current_session()
        if session is None:
            return PlaybackState.empty()

        playback_info = session.get_playback_info()
        timeline = session.get_timeline_properties()
        track = self._read_track_metadata(session)

        return PlaybackState(
            is_playing=_is_playing(playback_info.playback_status),
            track=track,
            position_ms=_timedelta_to_milliseconds(timeline.position),
            duration_ms=_timedelta_to_milliseconds(timeline.max_seek_time),
        )

    def send_command(self, command: str) -> str:
        session = self._get_current_session()
        if session is None:
            return "No active media session is available."

        if command == "prev":
            _run_async(session.try_skip_previous_async())
            return "Sent previous track command to the active session."
        if command == "next":
            _run_async(session.try_skip_next_async())
            return "Sent next track command to the active session."
        if command == "playpause":
            controls = session.get_playback_info().controls
            if controls.is_play_pause_toggle_enabled:
                _run_async(session.try_toggle_play_pause_async())
                return "Toggled play/pause on the active session."
            if _is_playing(session.get_playback_info().playback_status):
                _run_async(session.try_pause_async())
                return "Paused the active session."
            _run_async(session.try_play_async())
            return "Started playback on the active session."

        return f"Unsupported media command: {command}"

    def seek_position(self, position_ms: int) -> str:
        session = self._get_current_session()
        if session is None:
            return "No active media session is available."

        clamped_position_ms = max(0, int(position_ms))
        _run_async(session.try_change_playback_position_async(_milliseconds_to_ticks(clamped_position_ms)))
        return f"Seeked the active session to {clamped_position_ms} ms."

    def describe_track(self) -> TrackMetadata:
        session = self._get_current_session()
        if session is None:
            return TrackMetadata.empty()

        return self._read_track_metadata(session)


def create_native_media_adapter() -> NativeMediaAdapter:
    """Create the platform adapter if the Windows SDK is importable.

    The current workspace does not have a buildable winsdk install yet, so the
    fallback adapter is the honest default until the dependency/toolchain is
    available.
    """

    try:
        import winsdk  # type: ignore[import-not-found]  # noqa: F401
    except Exception as exc:  # pragma: no cover - environment-dependent
        return UnavailableNativeMediaAdapter(reason=f"winsdk unavailable: {exc.__class__.__name__}")

    return WinsdkNativeMediaAdapter.create()