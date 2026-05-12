using ProductivityTracker.Data;

namespace ProductivityTracker.Tracking;

internal sealed class ActivityTracker : IDisposable
{
    private readonly TrackerDatabase _database;
    private readonly AppSettings _settings;
    private readonly ActiveWindowReader _activeWindowReader;
    private readonly ScreenshotService _screenshotService;
    private readonly System.Threading.Timer _activityTimer;
    private readonly System.Threading.Timer _screenshotTimer;
    private ActiveWindowSnapshot? _current;
    private DateTimeOffset _currentStartedAt;
    private bool _started;

    public ActivityTracker(TrackerDatabase database, AppSettings settings)
    {
        _database = database;
        _settings = settings;
        _activeWindowReader = new ActiveWindowReader(settings);
        _screenshotService = new ScreenshotService(database, settings);
        _activityTimer = new System.Threading.Timer(_ => PollActiveWindow());
        _screenshotTimer = new System.Threading.Timer(_ => CaptureScreenshot());
    }

    public ActiveWindowSnapshot? Current => _current;

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _current = _activeWindowReader.Read();
        _currentStartedAt = DateTimeOffset.Now;
        _activityTimer.Change(TimeSpan.Zero, _settings.ActiveWindowPollInterval);
        _screenshotTimer.Change(_settings.ScreenshotInterval, _settings.ScreenshotInterval);
    }

    private void PollActiveWindow()
    {
        var next = _activeWindowReader.Read();
        var now = DateTimeOffset.Now;

        if (_current is null)
        {
            _current = next;
            _currentStartedAt = now;
            return;
        }

        if (IsSameActivity(_current, next))
        {
            _current = next;
            return;
        }

        SaveCurrent(now);
        _current = next;
        _currentStartedAt = now;
    }

    private void CaptureScreenshot()
    {
        var snapshot = _current ?? _activeWindowReader.Read();
        if (snapshot.IsIdle)
        {
            return;
        }

        _screenshotService.Capture(snapshot);
    }

    private void SaveCurrent(DateTimeOffset endedAt)
    {
        if (_current is null)
        {
            return;
        }

        if (endedAt - _currentStartedAt < TimeSpan.FromSeconds(1))
        {
            return;
        }

        _database.InsertAppActivity(new AppActivityRecord(
            _currentStartedAt,
            endedAt,
            Environment.UserName,
            _current.ProcessName,
            _current.ExecutablePath,
            _current.WindowTitle,
            _current.IsIdle));

        if (TryGetBrowserName(_current.ProcessName, out var browserName))
        {
            _database.InsertBrowserActivity(new BrowserActivityRecord(
                _currentStartedAt,
                endedAt,
                Environment.UserName,
                "fallback_window_title",
                browserName,
                string.Empty,
                string.Empty,
                _current.WindowTitle));
        }
    }

    private static bool IsSameActivity(ActiveWindowSnapshot left, ActiveWindowSnapshot right)
    {
        return string.Equals(left.ProcessName, right.ProcessName, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.WindowTitle, right.WindowTitle, StringComparison.Ordinal)
            && left.IsIdle == right.IsIdle;
    }

    private static bool TryGetBrowserName(string processName, out string browserName)
    {
        var normalized = Path.GetFileNameWithoutExtension(processName).ToLowerInvariant();
        browserName = normalized switch
        {
            "chrome" => "Chrome",
            "msedge" => "Edge",
            "firefox" => "Firefox",
            "brave" => "Brave",
            "opera" => "Opera",
            _ => string.Empty
        };

        return browserName.Length > 0;
    }

    public void ApplySettings(AppSettings settings)
    {
        _screenshotTimer.Change(settings.ScreenshotInterval, settings.ScreenshotInterval);
    }

    public void Dispose()
    {
        SaveCurrent(DateTimeOffset.Now);
        _activityTimer.Dispose();
        _screenshotTimer.Dispose();
    }
}
