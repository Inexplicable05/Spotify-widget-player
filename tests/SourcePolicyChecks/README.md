# Source and audio checks

From the repository root, run `dotnet run --project tests/SourcePolicyChecks`.
This checks allowed/blocked identities, process mappings, and clearing visualization on pause or a blocked source.

Add `-- --live` to list current Windows media session identities and matching audio process IDs. Track titles and artwork are not collected.

Add `-- --capture-check` to test real process isolation. This plays two quiet tones from separate test processes for about two seconds and verifies that capture includes the target tone while rejecting the other tone. Requires an audio output device and Windows build 20348 or later. Audio stays in memory.
