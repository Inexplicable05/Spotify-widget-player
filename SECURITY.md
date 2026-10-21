# Security policy

SpotiFloat is in development. There is no separate long-term support branch; security fixes target the main development branch. Use current source or a release explicitly identified as containing a fix.

## Report a vulnerability

Use GitHub's private vulnerability reporting on this repository when it is available. If it is not enabled, open an issue asking the maintainer for a private reporting channel, without publishing exploit details or sensitive data. Do not assume that a public issue is private.

Include the affected commit/version, Windows build, reproduction steps, impact and any suggested mitigation. Remove account information, private artwork, local usernames and tokens from attachments. Reports will be reviewed as maintainer availability permits; there is no promised response deadline.

## Local data and permissions

The widget reads Windows media sessions and uses selected-process audio loopback for visualization. Preferences and optional startup registration are stored for the current user. Cleanup and uninstall are explicit, confirmed actions; normal playback does not need administrator privileges.

The app does not ask for music-service passwords or API keys. Its GitHub button opens the browser. Read [Usage](docs/usage.md#privacy) for local diagnostic-output considerations.
