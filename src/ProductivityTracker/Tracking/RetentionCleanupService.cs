using ProductivityTracker.Data;

namespace ProductivityTracker.Tracking;

internal sealed class RetentionCleanupService : IDisposable
{
    private readonly TrackerDatabase _database;
    private readonly AppSettings _settings;
    private readonly System.Threading.Timer _timer;
    private bool _isRunning;

    public RetentionCleanupService(TrackerDatabase database, AppSettings settings)
    {
        _database = database;
        _settings = settings;
        _timer = new System.Threading.Timer(_ => Run());
    }

    public void Start()
    {
        _timer.Change(TimeSpan.FromMinutes(2), TimeSpan.FromHours(24));
    }

    private void Run()
    {
        if (_isRunning || _settings.ScreenshotRetentionDays <= 0)
        {
            return;
        }

        _isRunning = true;
        try
        {
            var cutoff = DateTimeOffset.Now.AddDays(-_settings.ScreenshotRetentionDays);
            DeleteOldScreenshotFiles(cutoff);
            _database.DeleteScreenshotMetadataOlderThan(cutoff);
            _database.SetState("lastScreenshotRetentionCleanupAt", DateTimeOffset.Now.ToString("O"));
        }
        catch (Exception ex)
        {
            _database.SetState("lastScreenshotRetentionCleanupError", ex.Message);
        }
        finally
        {
            _isRunning = false;
        }
    }

    private void DeleteOldScreenshotFiles(DateTimeOffset cutoff)
    {
        if (!Directory.Exists(_settings.ScreenshotDirectory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(_settings.ScreenshotDirectory, "*.jpg", SearchOption.AllDirectories))
        {
            try
            {
                var lastWrite = File.GetLastWriteTime(file);
                if (lastWrite < cutoff.LocalDateTime)
                {
                    File.Delete(file);
                }
            }
            catch
            {
            }
        }

        foreach (var directory in Directory.EnumerateDirectories(_settings.ScreenshotDirectory, "*", SearchOption.AllDirectories).OrderByDescending(path => path.Length))
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(directory).Any())
                {
                    Directory.Delete(directory);
                }
            }
            catch
            {
            }
        }
    }

    public void RunNow()
    {
        ThreadPool.QueueUserWorkItem(_ => Run());
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
