# SaltStack deployment

This folder contains example deployment artifacts.

## Recommended package flow

1. Build and publish a self-contained single EXE.
2. Place `1984.exe` and `1984.config.json` in Salt fileserver path:

```text
salt://1984/1984.exe
salt://1984/1984.config.json
```

3. Apply `init-1984.sls` to target users/endpoints.

## Notes

The included `init-1984.sls` state installs the executable to the user's `%LOCALAPPDATA%\Programs\1984` path and uses HKCU auto-start. In many Salt environments, HKCU depends on the context in which Salt runs. If Salt runs as LocalSystem, prefer invoking `install-productivity-tracker.ps1` in the target user's context or use your existing user-context deployment mechanism.

## Browser extension deployment

Deploy the Chrome/Edge extension using enterprise browser policies. This repository includes unpacked extension source for testing and packaging.
