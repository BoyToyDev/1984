# 1984

Lightweight local-only Windows employee productivity tracker for Windows 10/11.

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

## Build

Install .NET 8 SDK, then run:

```powershell
dotnet restore g:\AI\ProductivityTracker\ProductivityTracker.sln
dotnet build g:\AI\ProductivityTracker\ProductivityTracker.sln -c Release
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

## Reports

Right-click the tray icon and select `Generate today's report`.

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

Tray exit can be password-protected. Generate a production config with:

```powershell
g:\AI\ProductivityTracker\scripts\New-1984ExitPasswordConfig.ps1 -Password "change-me"
```

The generated config stores PBKDF2 hash + salt, not the plain password. Do not deploy `1984.config.example.json` if password-protected exit is required.

## SaltStack deployment

See `deploy/salt/init.sls` and `deploy/salt/install-productivity-tracker.ps1`.

Recommended per-user executable location:

```text
%LOCALAPPDATA%\Programs\1984\1984.exe
```

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
