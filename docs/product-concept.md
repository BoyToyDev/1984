# 1984 product concept

## Goal

1984 is a local-only Windows productivity tracker with a protected local interface. It runs in the tray, starts automatically for the current Windows user, records process activity, optional browser activity, screenshots, and allows authorized viewing/configuration through a password-protected UI.

## Startup flow

### First launch

1. The app starts normally.
2. If no local access password exists, the app opens a first-run setup window.
3. The user sets an interface password.
4. The password is stored only as PBKDF2 hash + salt, not as plain text.
5. The app writes/updates local configuration.
6. The app registers itself in current-user auto-start.
7. The app continues running in the tray.

### Normal launch

1. The app loads configuration.
2. The app initializes SQLite and runtime folders.
3. The app starts trackers.
4. The app registers or verifies current-user auto-start.
5. The app shows only the tray icon by default.

## User experience principles

The app should not create daily discomfort for employees.

Default behavior:

- no recurring popups
- no repeated permission prompts
- no browser heartbeat notifications
- no visible console windows
- no elevation/UAC request for normal per-user operation
- no modal windows unless the local UI is intentionally opened or first-run setup is required

The browser plugin heartbeat must be silent and background-only. It is only used by the app to understand whether full browser metadata is available.

## Tray behavior

The tray menu has two primary actions:

- Open application
- Close 1984

Both actions require the local access password.

If password verification fails, the action is rejected and the app continues running.

## Main application UI

The main UI is available only after password verification.

Recommended tabs:

### Dashboard

Shows current status:

- current Windows user
- tracking status
- current active process
- browser plugin status
- screenshot interval
- screenshot folder
- database location
- last screenshot time
- last browser event time

Recommended human-readable status cards:

- `Chrome was opened at 09:12, closed at 10:04, total 52 minutes`
- `Visual Studio Code is currently active, current session 18 minutes`
- `Browser plugin active, last event 2 minutes ago`
- `Limited web mode: browser plugin not detected`

Click behavior:

- clicking an application status card opens the Process history tab filtered by that process
- clicking a browser status card opens the Web history tab filtered by browser/domain when possible
- clicking screenshot status opens the Screenshots tab

### Process history

Shows application/window usage history.

Columns:

- Windows user
- started at
- ended at
- duration
- process name
- executable path
- window title
- idle flag

Useful filters:

- date range
- user
- process name
- idle/non-idle
- search by window title

### Web history

Shows browser activity from two sources:

1. browser plugin events
2. fallback process/window-title tracking when plugin is not installed or not active

Columns:

- Windows user
- source
- browser
- started at
- ended at
- duration
- tab title
- URL
- domain
- plugin event flag

When the plugin is not available, web history remains limited. The app can infer browser usage from active browser processes and window titles, but cannot reliably know URL or full tab details.

### Screenshots

Shows screenshot history.

Columns/data:

- captured at
- Windows user
- file path
- active process
- active window title
- idle flag
- thumbnail preview

Actions:

- open screenshot
- open screenshot folder
- filter by date

### Reports

Allows generating local HTML reports.

Options:

- report date
- include process summary
- include web summary
- include screenshots
- include idle periods

### Settings

Recommended settings:

- screenshot folder
- screenshot interval in minutes
- screenshot retention in days
- active window polling interval
- idle threshold in minutes
- browser receiver port
- retention period in days
- enable/disable screenshots
- enable/disable browser receiver
- auto-start enabled/disabled
- change local access password
- export report folder
- open data folder

Advanced settings:

- anonymize window titles in reports
- exclude process names from tracking
- exclude domains from web history
- screenshot image quality
- pause screenshots while idle
- delete local data older than retention period
- delete screenshots older than screenshot retention period

## Password/security model

There should be one local access password for:

- opening the main UI
- closing the tray app
- changing sensitive settings
- changing password

Password storage:

- PBKDF2-HMAC-SHA256
- random salt
- configurable iterations
- constant-time verification
- never store plain text

Recommended behavior:

- require current password before changing password
- lock settings after failed attempts for a short delay
- do not log entered passwords
- do not show password hash in UI

## Local data model additions

Existing activity tables should be extended with user/source information.

Recommended tables or fields:

### app_activity

- id
- windows_user
- started_at
- ended_at
- duration_seconds
- process_name
- executable_path
- window_title
- is_idle

### browser_activity

- id
- windows_user
- source
- browser
- started_at
- ended_at
- duration_seconds
- title
- url
- domain

`source` values:

- plugin
- fallback_window_title

### screenshots

- id
- windows_user
- captured_at
- file_path
- active_process
- active_window_title
- is_idle

### app_state

Useful for local app state:

- key
- value

Examples:

- firstRunCompleted
- lastPluginSeenAt
- lastAutoStartCheckAt
- lastScreenshotRetentionCleanupAt

## Screenshot retention

Screenshots can consume disk space quickly, so the app should support a separate screenshot retention period.

Recommended behavior:

- `screenshotRetentionDays` controls deletion of screenshot files
- default value: 30 days
- cleanup runs quietly in the background
- cleanup removes old screenshot metadata from SQLite after deleting files
- cleanup should not show popup notifications
- failures should be recorded locally and visible in the Settings/Diagnostics area

Database activity retention and screenshot file retention should be configurable separately.

## Browser plugin detection

The browser plugin should periodically send a lightweight heartbeat to the local receiver.

This heartbeat must be silent:

- no browser notification
- no app popup
- no visible tab
- no user prompt after initial extension deployment
- no content inspection

Heartbeat payload:

- type: heartbeat
- browser
- extension version
- timestamp

The app considers the plugin active if a heartbeat or browser event was seen recently.

If no plugin is detected:

- show limited mode in Dashboard and Web history
- explain that URL/tab history requires the plugin
- provide a button/instructions to install the browser extension

This warning should be shown only inside the password-protected UI, not as a popup to the employee.

## Web tracking compatibility

With plugin:

- URL is available
- tab title is available
- browser name is available
- accurate tab start/end duration is available

Without plugin:

- browser process usage is still recorded in Process history
- Web history can show limited inferred rows for known browser processes
- URL is unavailable
- tab title may be approximated from the active window title
- rows must clearly show source as fallback/limited

## Privacy and safety boundaries

1984 must not include:

- keylogging
- credential collection
- page content scraping
- OCR
- remote control
- covert operation
- cloud synchronization
- process injection

The tray icon should remain visible while running.

## Implementation phases

### Phase 1

- first-run password setup
- unified password verifier
- tray menu: Open application / Close 1984
- password required for both actions
- auto-start registration on launch

### Phase 2

- main WinForms UI with tabs
- Process history tab
- Web history tab
- Settings tab

### Phase 3

- configurable screenshot folder and interval
- change password flow
- plugin heartbeat and plugin status
- compatible web history with plugin/fallback sources

### Phase 4

- reports UI
- retention cleanup
- filters/search
- safety hardening
