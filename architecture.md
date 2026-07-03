Architecture Specification: Desktop Capsule Player

1. Overview

The Capsule Player is a lightweight, always-on-top desktop widget designed to provide seamless music playback controls and metadata visualization without interrupting user flow. To ensure maximum performance, zero network latency, and a minimal system footprint, the application bypasses standard REST APIs (like the Spotify Web API) and interfaces directly with the host operating system's native media components.

2. Technology Stack

The stack is selected to avoid heavy compilation environments (like Visual Studio C++ Build Tools) while maintaining a modern, hardware-accelerated UI.

Frontend UI: HTML5, CSS3, Vanilla JavaScript

Backend / Window Manager: Python 3

Webview Engine: PyWebView (utilizing native Windows WebView2)

OS Media Integration: winsdk (Windows System Media Transport Controls API)

Process Monitoring: psutil (for lifecycle management)

Image Processing: Pillow and color-thief (for adaptive UI colors)

3. Core Architecture & Data Flow

The architecture follows a Local System Hook pattern. There is no external internet traffic required for the core functionality.

[ Spotify.exe ]  <-- (Native OS Event) -->  [ Windows SMTC ]
                                                  |
                                                  v
[ PyWebView (Frontend) ]  <-- (JSON / Base64) -- [ Python Backend (Daemon) ]


The Daemon: A lightweight Python script runs in the background.

The Interceptor: Python hooks into the Windows SMTC (System Media Transport Controls) to read media events.

The Bridge: When an event occurs (e.g., song change), Python processes the data and injects it directly into the PyWebView JavaScript context via window.evaluate_js().

The UI: The HTML/CSS frontend updates instantly. User clicks on the UI are sent back to Python via window.pywebview.api, which then forwards native media commands back to Windows.

4. Feature Implementation Details

4.1. Media Controls & Playback State (Zero Latency)

Mechanism: When a user clicks "Play", "Pause", "Next", or "Previous", the frontend JS calls a Python API method.

Execution: Python uses winsdk.windows.media.control to send a hardware-level media key command to the OS.

Advantage: This completely bypasses the Spotify API rate limits and network latency. The action is instantaneous, identical to pressing a physical media key on a keyboard.

4.2. Metadata and Album Art Extraction

Mechanism: The Python backend listens to GlobalSystemMediaTransportControlsSessionManager.

Metadata: Track Title and Artist are pulled from try_get_media_properties_async().

Album Art: Windows caches the current Spotify album art as an IRandomAccessStream. Python reads this stream directly from memory, converts it into a Base64 string, and passes it to the HTML <img> tag. No file is ever saved to the hard drive.

4.3. Adaptive UI Styling (Dominant Color)

Mechanism: Before passing the album art Base64 string to the frontend, Python intercepts the image stream in memory.

Execution: The color-thief library analyzes the image stream to find the dominant RGB color.

UI Update: Python injects a CSS variable or directly updates the JavaScript window.themeColor property, which instantly themes the progress bar fill and other accent elements to match the current album cover.

4.4. Progress Bar Extrapolation (Offline Mode)

Problem: Polling the OS for the exact millisecond of playback 60 times a second is resource-heavy and can lag.

Mechanism: Python fetches the total duration of the song and the current timestamp only on specific events (Song Start, Play/Pause, User Scrub).

Execution: Python passes this data to the JavaScript frontend. The frontend uses a local requestAnimationFrame timer to visually extrapolate and fill the progress bar smoothly. It only resyncs with the backend if an anomaly is detected.

4.5. Lifecycle Automation (Autoboot & Auto-terminate)

Mechanism: A headless .pyw Python script runs silently on Windows startup.

Execution: Using psutil, the script polls the Windows process list every 2-3 seconds, looking for Spotify.exe.

Autoboot: If Spotify.exe is detected and the widget is not open, Python instantiates the PyWebView transparent window.

Auto-terminate: If the widget is open, but Spotify.exe disappears from the process list (user closed Spotify), the Python daemon executes window.destroy() to kill the UI and free up RAM, returning to its silent polling state.

5. UI/UX Specifications

Window State: Frameless (no title bar), Transparent (allows OS-level rendering behind it), and Always-On-Top.

Visual Style: Dark Mode Frosted Glass. Achieved using CSS backdrop-filter: blur(20px) saturate(150%) combined with a semi-transparent dark RGBA background.

Window Dragging: Enabled via the CSS property -webkit-app-region: drag; on the main container, explicitly disabled (-webkit-app-region: no-drag;) on interactive elements like buttons and sliders to prevent accidental window movement during use.