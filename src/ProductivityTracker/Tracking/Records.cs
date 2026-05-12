namespace ProductivityTracker.Tracking;

public sealed record AppActivityRecord(
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string WindowsUser,
    string ProcessName,
    string? ExecutablePath,
    string WindowTitle,
    bool IsIdle)
{
    public TimeSpan Duration => EndedAt - StartedAt;
}

public sealed record BrowserActivityRecord(
    DateTimeOffset StartedAt,
    DateTimeOffset EndedAt,
    string WindowsUser,
    string Source,
    string Browser,
    string Url,
    string Domain,
    string Title)
{
    public TimeSpan Duration => EndedAt - StartedAt;
}

public sealed record ScreenshotRecord(
    DateTimeOffset CapturedAt,
    string WindowsUser,
    string FilePath,
    string? ActiveProcess,
    string? ActiveWindowTitle,
    bool IsIdle);

internal sealed record ActiveWindowSnapshot(
    string ProcessName,
    string? ExecutablePath,
    string WindowTitle,
    bool IsIdle);
