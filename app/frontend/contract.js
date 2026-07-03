window.CapsulePlayerContract = Object.freeze({
    commands: Object.freeze({
        previous: 'prev',
        playPause: 'playpause',
        next: 'next',
    }),
    createEmptyState() {
        return {
            ready: false,
            status_message: 'Media bridge is to be built.',
            playback_state: {
                is_playing: false,
                track: null,
                position_ms: null,
                duration_ms: null,
            },
            last_command: null,
        };
    },
});