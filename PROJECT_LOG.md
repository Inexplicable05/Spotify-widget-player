# Project Log

## 2026-07-02
- Established the workspace as a Windows desktop Spotify widget project.
- Confirmed the current architecture direction: local native media integration with a PyWebView frontend.
- Started process scaffolding for phased development.
- Moved the phase plan into a dedicated [PHASES](PHASES) folder with numbered phase documents.
- Expanded Phase 02 into task-level documents so the first build phase has an explicit implementation list.
- Created the first application skeleton under `app/` with backend, frontend, and shared boundaries.
- Added backend config and logging scaffolding and replaced sample UI content with an honest empty-state shell.
- Validated the backend entry point by running `python -m app.backend.main` successfully.
- Added validated configuration loading, backend config errors, rotating file logging support, and environment variable documentation.
- Re-validated backend startup after config hardening and confirmed the foundation runtime still boots cleanly.
- Built the explicit frontend/backend contract and added an executable Phase 02 quality-gate script.
- Verified the new foundation gate with `tools/verify_phase_02.py` and confirmed it passes.
- Added a dedicated diagnostics helper for startup, shutdown, and failure logging.
- Completed Phase 02 foundation work and moved the build focus to Phase 03 native media integration.
- Created the Phase 03 task breakdown and the native media gateway boundary in the backend.
- Attempted to install `winsdk` and `psutil`; `winsdk` is blocked here by the missing MSVC/Visual Studio build toolchain.
- Converted the media gateway into an explicit native adapter boundary that reports unavailable state honestly.
- Added a Phase 03 verifier that checks the fallback native-media behavior without fabricating playback data.
- Documented the Python rollback rationale in `PYTHON_VERSION_ROLLBACK.md`.
- Rolled the workspace venv back to Python 3.11.15 and installed `winsdk` and `psutil` successfully.
- Updated the active workspace Python environment to `C:\Users\ashut\Documents\Spotify Widget\.venv\Scripts\python.exe`.
- Wired the frontend to render live playback state, album art, play/pause icons, and periodic refresh from the backend.
- Re-validated Phase 03 after the frontend update.
- Tightened the empty-state UI so missing media, title, artist, and artwork are shown explicitly instead of leaving stale values visible.
- Fixed live playback duration mapping from the Windows timeline and enabled smooth progress extrapolation in the frontend.
- Added a pywebview desktop launcher and wired the backend entry point to open the frameless transparent widget window.
- Verified the launcher module imports cleanly in the active virtual environment.

## Current Rules
- Prefer `MODE: DEEP_WORK` for implementation work.
- Prefer `MODE: CODE_REVIEW` for review tasks.
- Do not use dummy or fake data for unfinished features.
- Use explicit empty states or "to be built" messaging when data is unavailable.
- Record mistakes, risks, and process deviations in `MISTAKES_TO_AVOID.md`.
- Finish the current phase before starting the next one.
- Keep each numbered phase document detailed enough to guide implementation, checks, and avoidance rules.

## Next Actions
- Run the Phase 02 quality gate script.
- Move to the next foundation slice only after the contract and gate are confirmed.
- Continue with the remaining Phase 02 foundation tasks only if deeper runtime wiring is needed.
- Review whether the diagnostics boundary needs a small exception-state helper before moving to the next phase.
- Start Phase 03 planning and task breakdown before writing native media integration code.
- Fill in the native media gateway with real Windows session access in the next slice.
- Decide whether to add a Windows build-toolchain setup step or keep native media integration on an availability-only fallback until the toolchain exists.
- Run the Phase 03 verifier and decide whether to stop at the honest fallback boundary until a supported Windows toolchain is available.
- Install a supported Python version before attempting real `winsdk` integration.
- Begin replacing the fallback native media adapter with real Windows session calls now that `winsdk` is installed.
- Continue tightening the live UI experience if you want smoother progress updates or art loading states.
- If you want visual launch testing, run the backend entry point and confirm the widget window appears with the current live session.