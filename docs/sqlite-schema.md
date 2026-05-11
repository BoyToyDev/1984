# SQLite schema

Database path:

```text
%APPDATA%\1984\tracker.db
```

## app_activity

Stores active foreground application/window intervals.

```sql
CREATE TABLE app_activity (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    started_at TEXT NOT NULL,
    ended_at TEXT NOT NULL,
    duration_seconds INTEGER NOT NULL,
    process_name TEXT NOT NULL,
    executable_path TEXT,
    window_title TEXT NOT NULL,
    is_idle INTEGER NOT NULL DEFAULT 0
);
```

## browser_activity

Stores active browser tab metadata received from the Chrome/Edge extension.

```sql
CREATE TABLE browser_activity (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    started_at TEXT NOT NULL,
    ended_at TEXT NOT NULL,
    duration_seconds INTEGER NOT NULL,
    browser TEXT NOT NULL,
    url TEXT NOT NULL,
    title TEXT NOT NULL
);
```

## screenshots

Stores screenshot metadata. Image files are stored on disk, not as BLOBs.

```sql
CREATE TABLE screenshots (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    captured_at TEXT NOT NULL,
    file_path TEXT NOT NULL,
    active_process TEXT,
    active_window_title TEXT,
    is_idle INTEGER NOT NULL DEFAULT 0
);
```

## Indexes

```sql
CREATE INDEX idx_app_activity_started_at ON app_activity(started_at);
CREATE INDEX idx_browser_activity_started_at ON browser_activity(started_at);
CREATE INDEX idx_screenshots_captured_at ON screenshots(captured_at);
```
