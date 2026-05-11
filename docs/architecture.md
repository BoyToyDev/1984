# Architecture

## Components

- **TrayApplicationContext**: user-visible tray icon, report command, data folder command, auto-start command, exit command.
- **ActivityTracker**: schedules foreground window polling and screenshot capture.
- **ActiveWindowReader**: reads foreground window handle, process name, executable path, and title through Win32 APIs.
- **IdleDetector**: uses `GetLastInputInfo` to mark intervals as idle after the configured threshold.
- **ScreenshotService**: captures primary screen JPEG screenshots at a fixed interval, skipped while idle.
- **BrowserActivityReceiver**: accepts browser tab metadata on `127.0.0.1` only.
- **TrackerDatabase**: local SQLite storage with WAL mode.
- **HtmlReportGenerator**: creates readable daily HTML reports.

## Data flow

```text
Foreground window -> ActivityTracker -> TrackerDatabase -> HTML report
Browser extension -> Local HTTP receiver -> TrackerDatabase -> HTML report
Screenshot timer -> ScreenshotService -> image file + TrackerDatabase -> HTML report
```

## Privacy and compliance boundaries

The application is designed as a transparent corporate productivity tool, not stealth spyware:

- it has a tray icon
- it provides a local exit option
- it avoids keylogging
- it avoids page content scraping
- it stores data locally
- it does not upload data or accept remote commands

## Production hardening checklist

- sign the EXE
- package extension through enterprise browser policies
- document employee notice and retention policy
- tune screenshot interval and retention period
- restrict access to the local data folder through normal Windows user permissions
- test antivirus behavior before rollout
