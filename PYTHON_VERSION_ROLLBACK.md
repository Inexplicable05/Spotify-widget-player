# Python Version Rollback Note

## Summary
The backend should use an older supported Python version, not Python 3.14, for the native Windows media work in this project.

This is not a code-quality rollback. It is an environment compatibility rollback.

## Why This Is Needed
- `winsdk` is the package that provides Windows SDK bindings for the native media integration.
- In this workspace, `winsdk` did not install as a ready-made wheel for Python 3.14.
- Pip fell back to building the package from source.
- The source build requires Microsoft Visual C++ / Visual Studio build tools.
- This environment does not currently have that toolchain installed.

## Root Cause
The issue is a compatibility and packaging problem, not a bug in the application code.

The combination that failed was:
- Python 3.14 interpreter
- Windows-only native SDK package
- No compatible prebuilt wheel for the current interpreter/platform combination
- No MSVC build toolchain available for source compilation

## Symptoms Observed
- `pip install winsdk` attempted a source build.
- The build failed while looking for Visual Studio / MSVC.
- The native media gateway could only run in fallback mode.
- The project could not move from an honest availability-only adapter to real Windows media access.

## Recommended Version Path
Use a supported Python release that is more likely to work with the current Windows package ecosystem, such as:
- Python 3.12 x64
- Python 3.11 x64

Prefer one of those versions for the backend until the dependency stack has native support for Python 3.14.

## Correct Fix Path
1. Install Python 3.12 x64 or 3.11 x64.
2. Create a new virtual environment with that Python.
3. Install `winsdk` and `psutil` there.
4. Confirm `winsdk` imports successfully.
5. Resume Phase 03 native media integration work.

## What Not To Do
- Do not keep forcing Python 3.14 for this dependency stack.
- Do not fake Windows media availability in the code to avoid the install issue.
- Do not mark Phase 03 complete while the adapter is still in fallback mode.

## Validation Check
The environment is acceptable only when the following succeeds without a fallback-only adapter:

```powershell
python -c "from winsdk.windows.media.control import GlobalSystemMediaTransportControlsSessionManager"
```

## Status
Current status: resolved in this workspace by rolling back to Python 3.11.15 and installing `winsdk` successfully.