const capsuleRoot = document.getElementById('player-capsule');
const albumArt = document.getElementById('ui-album-art');
const statusText = document.getElementById('ui-status');
const titleText = document.getElementById('ui-title');
const artistText = document.getElementById('ui-artist');
const progressBar = document.getElementById('ui-progress');
const prevButton = document.getElementById('btn-prev');
const playPauseButton = document.getElementById('btn-playpause');
const nextButton = document.getElementById('btn-next');
const playPauseIconPath = playPauseButton.querySelector('path');
const POLL_INTERVAL_MS = 5000;
const FRAME_INTERVAL_MS = 1000 / 60;
const COMMAND_CONFIRMATION_TIMEOUT_MS = 5000;
const SEEK_CONFIRMATION_TIMEOUT_MS = 5000;
const SEEK_CONFIRMATION_TOLERANCE_MS = 1000;

let latestState = window.CapsulePlayerContract.createEmptyState();
let latestStateReceivedAt = performance.now();
let progressAnimationFrameId = null;
let pendingPlaybackIntent = null;
let isSeeking = false;
let pendingSeekPositionMs = null;
let pendingSeekTargetMs = null;
let pendingSeekExpiresAt = null;
let seekResyncTimeoutId = null;

function formatProgressLabel(positionMs, durationMs) {
    if (!durationMs || durationMs <= 0) {
        return '0:00';
    }

    const totalSeconds = Math.max(0, Math.floor(positionMs / 1000));
    const totalMinutes = Math.floor(totalSeconds / 60);
    const totalRemainder = totalSeconds % 60;
    const durationSeconds = Math.max(0, Math.floor(durationMs / 1000));
    const durationMinutes = Math.floor(durationSeconds / 60);
    const durationRemainder = durationSeconds % 60;
    return `${String(totalMinutes)}:${String(totalRemainder).padStart(2, '0')} / ${String(durationMinutes)}:${String(durationRemainder).padStart(2, '0')}`;
}

function clampProgressMs(positionMs, durationMs) {
    if (!durationMs || durationMs <= 0) {
        return 0;
    }

    return Math.min(durationMs, Math.max(0, positionMs));
}

function getRenderedPlaybackState() {
    const playbackState = latestState?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const durationMs = playbackState.duration_ms;
    const basePositionMs = playbackState.position_ms ?? 0;
    if (!latestState?.ready || !playbackState.is_playing || !durationMs || durationMs <= 0) {
        return { playbackState, positionMs: clampProgressMs(basePositionMs, durationMs) };
    }

    const elapsedMs = performance.now() - latestStateReceivedAt;
    const renderedPositionMs = clampProgressMs(basePositionMs + elapsedMs, durationMs);
    return {
        playbackState: {
            ...playbackState,
            position_ms: renderedPositionMs,
        },
        positionMs: renderedPositionMs,
    };
}

function setAlbumArt(track) {
    const albumArtUri = track?.album_art_data_uri;
    if (albumArtUri) {
        albumArt.src = albumArtUri;
        albumArt.alt = `${track?.title ?? 'Current track'} album art`;
        albumArt.classList.add('is-image');
        albumArt.textContent = '';
        return;
    }

    albumArt.removeAttribute('src');
    albumArt.alt = 'Album art unavailable';
    albumArt.classList.remove('is-image');
    albumArt.textContent = 'No art';
}

function setPlaybackAvailability(isReady) {
    prevButton.disabled = !isReady;
    playPauseButton.disabled = !isReady;
    nextButton.disabled = !isReady;
    progressBar.disabled = !isReady || !Number.isFinite(latestState?.playback_state?.duration_ms) || latestState.playback_state.duration_ms <= 0;
    capsuleRoot.dataset.ready = isReady ? 'true' : 'false';
}

function getPlaybackDurationMs(state) {
    return state?.playback_state?.duration_ms ?? null;
}

function getDisplayedPositionMs(playbackState) {
    if (pendingSeekTargetMs !== null) {
        return pendingSeekTargetMs;
    }

    if (isSeeking && pendingSeekPositionMs !== null) {
        return pendingSeekPositionMs;
    }

    return playbackState.position_ms ?? 0;
}

function setSeekPreview(positionMs, durationMs) {
    pendingSeekPositionMs = clampProgressMs(positionMs, durationMs);
    const progressPercent = Math.min(100, Math.max(0, Math.round(pendingSeekPositionMs * 100 / durationMs)));
    progressBar.value = String(progressPercent);
    progressBar.setAttribute('aria-valuetext', formatProgressLabel(pendingSeekPositionMs, durationMs));
}

function beginSeeking() {
    isSeeking = true;
}

function endSeeking() {
    isSeeking = false;
    pendingSeekPositionMs = null;
}

function beginSeekCommit(positionMs) {
    pendingSeekTargetMs = positionMs;
    pendingSeekExpiresAt = performance.now() + SEEK_CONFIRMATION_TIMEOUT_MS;
    if (seekResyncTimeoutId !== null) {
        window.clearTimeout(seekResyncTimeoutId);
        seekResyncTimeoutId = null;
    }
}

function clearPendingSeek() {
    pendingSeekTargetMs = null;
    pendingSeekExpiresAt = null;
    if (seekResyncTimeoutId !== null) {
        window.clearTimeout(seekResyncTimeoutId);
        seekResyncTimeoutId = null;
    }
}

function shouldHoldOptimisticSeekState(state) {
    if (pendingSeekTargetMs === null) {
        return false;
    }

    const playbackState = state?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const actualPositionMs = playbackState.position_ms ?? 0;
    if (Math.abs(actualPositionMs - pendingSeekTargetMs) <= SEEK_CONFIRMATION_TOLERANCE_MS) {
        clearPendingSeek();
        return false;
    }

    if (pendingSeekExpiresAt !== null && performance.now() <= pendingSeekExpiresAt) {
        return true;
    }

    clearPendingSeek();
    return false;
}

function setPlayPauseIcon(isPlaying) {
    if (isPlaying) {
        playPauseIconPath.setAttribute('d', 'M6,5H10V19H6V5M14,5H18V19H14V5Z');
        playPauseButton.setAttribute('aria-label', 'Pause');
        return;
    }

    playPauseIconPath.setAttribute('d', 'M8,5.14V19.14L19,12.14L8,5.14Z');
    playPauseButton.setAttribute('aria-label', 'Play');
}

function applyOptimisticCommandState(command) {
    const currentPlaybackState = latestState?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    if (command !== window.CapsulePlayerContract.commands.playPause) {
        return;
    }

    const nextPlaybackState = {
        ...currentPlaybackState,
        is_playing: !currentPlaybackState.is_playing,
    };
    latestState = {
        ...latestState,
        playback_state: nextPlaybackState,
    };
    latestStateReceivedAt = performance.now();
    renderPlaybackState(latestState);
}

function setPendingPlaybackIntent(expectedIsPlaying) {
    pendingPlaybackIntent = {
        expectedIsPlaying,
        expiresAt: performance.now() + COMMAND_CONFIRMATION_TIMEOUT_MS,
    };
}

function clearPendingPlaybackIntent() {
    pendingPlaybackIntent = null;
}

function shouldHoldOptimisticPlaybackState(state) {
    if (!pendingPlaybackIntent) {
        return false;
    }

    const actualPlaybackState = state?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    if (actualPlaybackState.is_playing === pendingPlaybackIntent.expectedIsPlaying) {
        clearPendingPlaybackIntent();
        return false;
    }

    if (performance.now() <= pendingPlaybackIntent.expiresAt) {
        return true;
    }

    clearPendingPlaybackIntent();
    return false;
}

function renderPlaybackState(state) {
    const playbackState = state?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const track = playbackState.track;
    const hasDuration = Number.isFinite(playbackState.duration_ms) && playbackState.duration_ms > 0;
    const displayedPositionMs = getDisplayedPositionMs(playbackState);
    const currentProgress = hasDuration
        ? Math.min(100, Math.max(0, Math.round(displayedPositionMs * 100 / playbackState.duration_ms)))
        : 0;
    const isReady = Boolean(state?.ready);
    const hasTrack = Boolean(track?.title || track?.artist);

    titleText.textContent = hasTrack ? (track?.title ?? 'Unknown track') : (isReady ? 'No track information' : 'To be built');
    artistText.textContent = hasTrack ? (track?.artist ?? 'Unknown artist') : (isReady ? 'No media session available' : 'No media data available yet');
    statusText.textContent = state?.status_message ?? (isReady ? 'Native media integration is active.' : 'Media bridge is to be built.');
    if (!isSeeking) {
        progressBar.value = String(currentProgress);
    }
    progressBar.disabled = !isReady || !hasDuration;
    capsuleRoot.dataset.playing = playbackState.is_playing ? 'true' : 'false';
    setAlbumArt(track);
    setPlayPauseIcon(Boolean(playbackState.is_playing));

    const progressLabel = formatProgressLabel(displayedPositionMs, playbackState.duration_ms ?? 0);
    progressBar.setAttribute('aria-valuetext', progressLabel);

    setPlaybackAvailability(isReady);
}

function tickProgress() {
    if (isSeeking) {
        progressAnimationFrameId = window.requestAnimationFrame(tickProgress);
        return;
    }

    const { playbackState, positionMs } = getRenderedPlaybackState();
    const hasDuration = Number.isFinite(playbackState.duration_ms) && playbackState.duration_ms > 0;
    if (!hasDuration) {
        progressAnimationFrameId = window.requestAnimationFrame(tickProgress);
        return;
    }

    const renderedProgress = Math.min(100, Math.max(0, Math.round(positionMs * 100 / playbackState.duration_ms)));
    progressBar.value = String(renderedProgress);
    progressBar.setAttribute('aria-valuetext', formatProgressLabel(positionMs, playbackState.duration_ms));
    progressAnimationFrameId = window.requestAnimationFrame(tickProgress);
}

function getSeekPositionFromBarValue(value) {
    const playbackState = latestState?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const durationMs = playbackState.duration_ms ?? 0;
    const percent = Math.min(100, Math.max(0, Number(value)));
    return clampProgressMs((percent * durationMs) / 100, durationMs);
}

async function requestState() {
    if (!window.pywebview?.api?.get_ui_state) {
        renderPlaybackState(window.CapsulePlayerContract.createEmptyState());
        return;
    }

    try {
        const state = await window.pywebview.api.get_ui_state();
        if (shouldHoldOptimisticSeekState(state)) {
            return;
        }

        if (shouldHoldOptimisticPlaybackState(state)) {
            return;
        }

        latestState = state ?? window.CapsulePlayerContract.createEmptyState();
        latestStateReceivedAt = performance.now();
        renderPlaybackState(state);
    } catch (error) {
        console.error(error);
        latestState = window.CapsulePlayerContract.createEmptyState();
        latestStateReceivedAt = performance.now();
        renderPlaybackState(latestState);
    }
}

async function sendCommand(command) {
    if (!window.pywebview?.api?.media_command) {
        statusText.textContent = 'Media bridge is to be built.';
        return;
    }

    const currentPlaybackState = latestState?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const expectedIsPlaying = command === window.CapsulePlayerContract.commands.playPause
        ? !currentPlaybackState.is_playing
        : null;

    applyOptimisticCommandState(command);
    if (command === window.CapsulePlayerContract.commands.playPause) {
        setPendingPlaybackIntent(Boolean(expectedIsPlaying));
    }

    window.requestAnimationFrame(() => {
        void sendCommandToBackend(command);
    });
}

async function sendCommandToBackend(command) {
    try {
        const result = await window.pywebview.api.media_command(command);
        if (result && result.accepted === false) {
            statusText.textContent = result.message ?? 'Command could not be sent.';
            clearPendingPlaybackIntent();
            void requestState();
        }
    } catch (error) {
        console.error(error);
        statusText.textContent = 'Command could not be sent.';
        clearPendingPlaybackIntent();
        void requestState();
    }
}

async function sendSeekToBackend(positionMs) {
    if (!window.pywebview?.api?.seek_position) {
        return;
    }

    try {
        const result = await window.pywebview.api.seek_position(Math.round(positionMs));
        if (result && result.accepted === false) {
            statusText.textContent = result.message ?? 'Seek could not be sent.';
            clearPendingSeek();
            void requestState();
            return;
        }
        seekResyncTimeoutId = window.setTimeout(() => {
            seekResyncTimeoutId = null;
            clearPendingSeek();
            void requestState();
        }, SEEK_CONFIRMATION_TIMEOUT_MS);
    } catch (error) {
        console.error(error);
        statusText.textContent = 'Seek could not be sent.';
        clearPendingSeek();
        void requestState();
    }
}

progressBar.addEventListener('pointerdown', () => {
    beginSeeking();
});

progressBar.addEventListener('input', () => {
    const playbackState = latestState?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const durationMs = playbackState.duration_ms;
    if (!durationMs || durationMs <= 0) {
        return;
    }

    beginSeeking();
    setSeekPreview(getSeekPositionFromBarValue(progressBar.value), durationMs);
});

progressBar.addEventListener('change', () => {
    const playbackState = latestState?.playback_state ?? window.CapsulePlayerContract.createEmptyState().playback_state;
    const durationMs = playbackState.duration_ms;
    if (!durationMs || durationMs <= 0) {
        endSeeking();
        return;
    }

    const seekPositionMs = getSeekPositionFromBarValue(progressBar.value);
    setSeekPreview(seekPositionMs, durationMs);
    latestState = {
        ...latestState,
        playback_state: {
            ...playbackState,
            position_ms: seekPositionMs,
        },
    };
    latestStateReceivedAt = performance.now();
    beginSeekCommit(seekPositionMs);
    endSeeking();
    void sendSeekToBackend(seekPositionMs);
});

progressBar.addEventListener('pointercancel', () => {
    endSeeking();
    void requestState();
});

prevButton.addEventListener('click', () => sendCommand(window.CapsulePlayerContract.commands.previous));
playPauseButton.addEventListener('click', () => sendCommand(window.CapsulePlayerContract.commands.playPause));
nextButton.addEventListener('click', () => sendCommand(window.CapsulePlayerContract.commands.next));

requestState();
setInterval(requestState, POLL_INTERVAL_MS);
progressAnimationFrameId = window.requestAnimationFrame(tickProgress);