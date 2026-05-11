using Microsoft.Data.Sqlite;
using ProductivityTracker.Tracking;

namespace ProductivityTracker.Data;

public sealed class TrackerDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly object _sync = new();

    public TrackerDatabase(string databasePath)
    {
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };

        _connection = new SqliteConnection(builder.ToString());
        _connection.Open();
    }

    public void Initialize()
    {
        ExecuteNonQuery("PRAGMA journal_mode=WAL;");
        ExecuteNonQuery("PRAGMA synchronous=NORMAL;");
        ExecuteNonQuery("PRAGMA foreign_keys=ON;");

        ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS app_activity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                started_at TEXT NOT NULL,
                ended_at TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                process_name TEXT NOT NULL,
                executable_path TEXT,
                window_title TEXT NOT NULL,
                is_idle INTEGER NOT NULL DEFAULT 0
            );
            """);

        ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS browser_activity (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                started_at TEXT NOT NULL,
                ended_at TEXT NOT NULL,
                duration_seconds INTEGER NOT NULL,
                browser TEXT NOT NULL,
                url TEXT NOT NULL,
                title TEXT NOT NULL
            );
            """);

        ExecuteNonQuery("""
            CREATE TABLE IF NOT EXISTS screenshots (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                captured_at TEXT NOT NULL,
                file_path TEXT NOT NULL,
                active_process TEXT,
                active_window_title TEXT,
                is_idle INTEGER NOT NULL DEFAULT 0
            );
            """);

        ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx_app_activity_started_at ON app_activity(started_at);");
        ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx_browser_activity_started_at ON browser_activity(started_at);");
        ExecuteNonQuery("CREATE INDEX IF NOT EXISTS idx_screenshots_captured_at ON screenshots(captured_at);");
    }

    public void InsertAppActivity(AppActivityRecord record)
    {
        lock (_sync)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                INSERT INTO app_activity (started_at, ended_at, duration_seconds, process_name, executable_path, window_title, is_idle)
                VALUES ($started_at, $ended_at, $duration_seconds, $process_name, $executable_path, $window_title, $is_idle);
                """;
            command.Parameters.AddWithValue("$started_at", record.StartedAt.ToString("O"));
            command.Parameters.AddWithValue("$ended_at", record.EndedAt.ToString("O"));
            command.Parameters.AddWithValue("$duration_seconds", (long)Math.Max(0, record.Duration.TotalSeconds));
            command.Parameters.AddWithValue("$process_name", record.ProcessName);
            command.Parameters.AddWithValue("$executable_path", (object?)record.ExecutablePath ?? DBNull.Value);
            command.Parameters.AddWithValue("$window_title", record.WindowTitle);
            command.Parameters.AddWithValue("$is_idle", record.IsIdle ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    public void InsertBrowserActivity(BrowserActivityRecord record)
    {
        lock (_sync)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                INSERT INTO browser_activity (started_at, ended_at, duration_seconds, browser, url, title)
                VALUES ($started_at, $ended_at, $duration_seconds, $browser, $url, $title);
                """;
            command.Parameters.AddWithValue("$started_at", record.StartedAt.ToString("O"));
            command.Parameters.AddWithValue("$ended_at", record.EndedAt.ToString("O"));
            command.Parameters.AddWithValue("$duration_seconds", (long)Math.Max(0, record.Duration.TotalSeconds));
            command.Parameters.AddWithValue("$browser", record.Browser);
            command.Parameters.AddWithValue("$url", record.Url);
            command.Parameters.AddWithValue("$title", record.Title);
            command.ExecuteNonQuery();
        }
    }

    public void InsertScreenshot(ScreenshotRecord record)
    {
        lock (_sync)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = """
                INSERT INTO screenshots (captured_at, file_path, active_process, active_window_title, is_idle)
                VALUES ($captured_at, $file_path, $active_process, $active_window_title, $is_idle);
                """;
            command.Parameters.AddWithValue("$captured_at", record.CapturedAt.ToString("O"));
            command.Parameters.AddWithValue("$file_path", record.FilePath);
            command.Parameters.AddWithValue("$active_process", (object?)record.ActiveProcess ?? DBNull.Value);
            command.Parameters.AddWithValue("$active_window_title", (object?)record.ActiveWindowTitle ?? DBNull.Value);
            command.Parameters.AddWithValue("$is_idle", record.IsIdle ? 1 : 0);
            command.ExecuteNonQuery();
        }
    }

    public IReadOnlyList<Dictionary<string, object?>> Query(string sql, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        lock (_sync)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            if (parameters is not null)
            {
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Key, parameter.Value ?? DBNull.Value);
                }
            }

            using var reader = command.ExecuteReader();
            var rows = new List<Dictionary<string, object?>>();
            while (reader.Read())
            {
                var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                }
                rows.Add(row);
            }

            return rows;
        }
    }

    private void ExecuteNonQuery(string sql)
    {
        lock (_sync)
        {
            using var command = _connection.CreateCommand();
            command.CommandText = sql;
            command.ExecuteNonQuery();
        }
    }

    public void Dispose() => _connection.Dispose();
}
