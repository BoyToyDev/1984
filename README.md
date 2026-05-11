# 1984

Lightweight local-only Windows employee productivity tracker for Windows 10/11.

1984 is designed to be quiet during normal employee work: no recurring popups, no repeated permission prompts, no browser heartbeat notifications, and no UAC prompt for normal per-user deployment.

## Scope

The application is designed for simple corporate deployment and low maintenance:

- WinForms tray application on .NET 8
- local SQLite storage
- active application/window tracking
- browser tab metadata tracking through a Chrome/Edge extension
- periodic screenshots
- idle time detection
- readable daily HTML reports
- optional current-user auto-start
- SaltStack deployment example
- password-protected local interface
- quiet browser plugin heartbeat detection
- configurable screenshot retention

## Explicit non-goals

This project intentionally does not implement:

- cloud/server infrastructure
- synchronization
- live monitoring
- remote control
- OCR
- AI analytics
- keyboard logging
- hidden spyware behavior
- page content scraping

## Responsible use

This project is intended for transparent, lawful corporate use with employee notice and an internal policy. See `ETHICS.md` and `SECURITY.md` before deployment.

See also:

- `docs/product-concept.md` for the detailed product concept
- `docs/product-ideas.md` for planned UX/security/reporting improvements

## Architecture

```text
1984.exe
├─ WinForms tray UI
├─ ActivityTracker
│  ├─ ActiveWindowReader: foreground process/window title
│  ├─ IdleDetector: GetLastInputInfo
│  └─ ScreenshotService: periodic JPEG screenshots
├─ BrowserActivityReceiver
│  └─ local HTTP endpoint on 127.0.0.1:39877
├─ TrackerDatabase
│  └─ SQLite database in LocalAppData
└─ HtmlReportGenerator
   └─ daily local HTML reports

Chrome/Edge extension
└─ sends URL/title/start/end timestamps to local app only
```

## Project structure

```text
ProductivityTracker/
├─ src/ProductivityTracker/          # .NET 8 WinForms tray application
├─ browser-extension/                # Chrome/Edge Manifest V3 extension
├─ deploy/salt/                      # SaltStack deployment example
├─ docs/                             # architecture and schema docs
├─ ProductivityTracker.sln
└─ README.md
```

## Local data paths

Default per-user data directory:

```text
%APPDATA%\1984
```

Important files:

```text
tracker.db
screenshots\YYYY-MM-DD\screenshot-HHMMSS.jpg
reports\report-YYYY-MM-DD.html
```

## Recommended libraries

- `Microsoft.Data.Sqlite` for SQLite access
- WinForms built-in `NotifyIcon` for tray UI
- Win32 APIs through P/Invoke for foreground window and idle detection
- `System.Drawing` via WinForms for screenshots on Windows

## Requirements

### Development machine

- Windows 10/11
- Git for Windows
- .NET 8 SDK
- Visual Studio 2022 or another editor with .NET support
- Chrome or Edge for browser extension testing

### Target employee machine

If published as a self-contained single EXE:

- Windows 10/11
- no .NET runtime installation required
- no admin rights required for per-user install
- Chrome or Edge extension only if full URL/tab history is required

If running a framework-dependent build:

- Windows 10/11
- .NET 8 Desktop Runtime

Normal recommended deployment is self-contained single EXE.

## Local development

Install .NET 8 SDK, then run:

```powershell
dotnet restore g:\AI\ProductivityTracker\ProductivityTracker.sln
dotnet build g:\AI\ProductivityTracker\ProductivityTracker.sln -c Release
```

Run from source:

```powershell
dotnet run --project g:\AI\ProductivityTracker\src\ProductivityTracker\ProductivityTracker.csproj
```

## Publish single EXE

```powershell
dotnet publish g:\AI\ProductivityTracker\src\ProductivityTracker\ProductivityTracker.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  /p:PublishSingleFile=true `
  /p:IncludeNativeLibrariesForSelfExtract=true `
  /p:EnableCompressionInSingleFile=true `
  /p:PublishReadyToRun=true `
  -o g:\AI\ProductivityTracker\publish\win-x64
```

## Browser extension

The extension records only:

- URL
- page title
- start timestamp
- end timestamp

It sends this metadata to:

```text
http://127.0.0.1:39877/browser-activity
```

The extension should also send a silent heartbeat so the desktop app can show whether full browser tracking is available. The heartbeat must not display notifications or interrupt the employee.

If the extension is not installed, 1984 still records browser process usage through normal process/window tracking, but URL-level web history is limited. The UI should show this only inside the password-protected application, not as a popup.

## Application UI

The tray icon has two primary actions:

- `Open application`
- `Close 1984`

Both actions require the local access password.

The main UI should be human-readable and suitable for quick review:

- **Dashboard**: status cards such as `Chrome was opened at 09:12, closed at 10:04, total 52 minutes`.
- **Processes**: process history with start time, end time, duration, user, process name, executable path, and window title.
- **Web**: browser history with user, title, URL, tab start time, duration, browser, and source.
- **Screenshots**: screenshot list and preview/open actions.
- **Reports**: generate local HTML reports.
- **Settings**: screenshot folder, screenshot interval, screenshot retention, password change, retention, browser status, and local folders.

Recommended dashboard behavior:

- clicking a Chrome/Edge status card opens the Web tab
- clicking an application card opens the Processes tab filtered by that process
- clicking screenshot status opens the Screenshots tab

## Reports

Reports are generated from the password-protected application UI.

The report includes:

- daily application usage summary
- browser usage summary
- active window timeline
- screenshots gallery

## Install layout

The compiled app is designed to run from any folder. Recommended per-user install folder:

```text
%LOCALAPPDATA%\Programs\1984
```

Avoid using `C:\ProgramData\1984` as the default install path because it is machine-wide and does not belong to the current user profile.

Runtime client data remains per-user in:

```text
%APPDATA%\1984
```

The app reads install config from:

```text
1984.config.json
```

placed next to `1984.exe`.

Final layout:

```text
%LOCALAPPDATA%\Programs\1984\1984.exe
%LOCALAPPDATA%\Programs\1984\1984.config.json
%APPDATA%\1984\tracker.db
```

## Exit password

The local access password protects:

- opening the application UI
- closing the tray app
- changing sensitive settings
- changing the password

On first launch, if no password exists, the app asks to create one. For managed deployment, generate a production config with:

```powershell
g:\AI\ProductivityTracker\scripts\New-1984ExitPasswordConfig.ps1 -Password "change-me"
```

The generated config stores PBKDF2 hash + salt, not the plain password. Do not deploy `1984.config.example.json` if password-protected exit is required.

## Configuration

Example config:

```json
{
  "productName": "1984",
  "companyName": "Organization",
  "dataDirectory": "%APPDATA%\\1984",
  "screenshotDirectory": "%APPDATA%\\1984\\screenshots",
  "activeWindowPollIntervalSeconds": 2,
  "screenshotIntervalSeconds": 300,
  "idleThresholdSeconds": 180,
  "browserReceiverPort": 39877,
  "retentionDays": 90,
  "screenshotRetentionDays": 30,
  "quietMode": true,
  "showNotifications": false,
  "browserPluginHeartbeatIntervalSeconds": 60
}
```

Important defaults:

- `quietMode`: keeps normal operation silent.
- `showNotifications`: should stay `false` for employee comfort.
- `screenshotRetentionDays`: removes old screenshot files after the configured number of days.
- `retentionDays`: controls activity database/report retention separately.

## Quiet operation

Normal employee experience should be:

1. App starts with Windows.
2. Tray icon is visible.
3. No recurring popups are shown.
4. No UAC prompt is shown for per-user deployment.
5. Browser heartbeat runs silently in the background.
6. Password prompt appears only when someone intentionally opens the UI or closes the app.

Do not use scheduled tasks or install locations that require elevation unless your environment explicitly needs machine-wide deployment.

## SaltStack deployment

See `deploy/salt/init.sls` and `deploy/salt/install-productivity-tracker.ps1`.

Recommended per-user executable location:

```text
%LOCALAPPDATA%\Programs\1984\1984.exe
```

Recommended deployment flow:

1. Publish self-contained `1984.exe`.
2. Generate production `1984.config.json`.
3. Copy both files to `%LOCALAPPDATA%\Programs\1984` for the target user.
4. Register HKCU auto-start.
5. Deploy the browser extension through Chrome/Edge enterprise policies if URL-level web history is required.
6. Validate that `%APPDATA%\1984` is created for runtime data.

If Salt runs as `LocalSystem`, HKCU and `%LOCALAPPDATA%` may refer to the system profile. Use a user-context deployment mechanism for per-user installs.

## Antivirus-safe practices

- visible tray icon and exit option
- no keyboard logging
- no credential collection
- no process injection
- no browser content scraping
- no remote command/control channel
- no persistence outside normal user auto-start
- signed executable recommended for production
- clear internal documentation and employee policy notice recommended

## Performance considerations

- foreground window polling defaults to every 2 seconds
- screenshots default to every 5 minutes and are skipped while idle
- SQLite uses WAL mode and short write transactions
- reports are generated on demand
- browser extension batches activity by tab/window changes and periodic flush

## Implementation status

This repository is a practical MVP scaffold. Before production rollout, test on representative Windows 10/11 endpoints, add code signing, tune screenshot retention, and package the browser extension with enterprise policies.
