# Deployment layout for 1984

## Recommended model

The executable folder and the per-user data folder are intentionally separate.

## Per-user install

Recommended executable folder:

```text
%LOCALAPPDATA%\Programs\1984\
```

Contents:

```text
1984.exe
1984.config.json
browser-extension\
```

Notes:

- This path belongs to the current Windows user profile.
- This normally does not require admin rights.
- This is the recommended model for shared computers.
- Runtime data is not written to this folder.

## Why not ProgramData

`C:\ProgramData\1984` is a machine-wide location and does not belong to a specific user profile. It is not the recommended install path for this project because the application should be deployed and started per user.

## Runtime data

Default runtime data folder for each Windows user:

```text
%APPDATA%\1984\
```

Contents:

```text
tracker.db
screenshots\YYYY-MM-DD\*.jpg
reports\report-YYYY-MM-DD.html
```

This design works better on shared computers because each user's executable/config path and activity data are separated by Windows profile.

## Final per-user layout

```text
%LOCALAPPDATA%\Programs\1984\
├─ 1984.exe
└─ 1984.config.json

%APPDATA%\1984\
├─ tracker.db
├─ screenshots\
└─ reports\
```

## Config

The app reads install config from the executable folder:

```text
1984.config.json
```

The config controls:

- product name
- company name
- per-user data directory
- polling intervals
- screenshot interval
- idle threshold
- local browser receiver port
- retention days
- exit password hash

The exit password should be stored as PBKDF2 hash + salt, not plain text.
