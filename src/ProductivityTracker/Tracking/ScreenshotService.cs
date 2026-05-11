using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using ProductivityTracker.Data;

namespace ProductivityTracker.Tracking;

internal sealed class ScreenshotService
{
    private readonly TrackerDatabase _database;
    private readonly AppSettings _settings;

    public ScreenshotService(TrackerDatabase database, AppSettings settings)
    {
        _database = database;
        _settings = settings;
    }

    public void Capture(ActiveWindowSnapshot activeWindow)
    {
        var now = DateTimeOffset.Now;
        var dayDirectory = Path.Combine(_settings.ScreenshotDirectory, now.ToString("yyyy-MM-dd"));
        Directory.CreateDirectory(dayDirectory);

        var fileName = $"screenshot-{now:HHmmss}.jpg";
        var path = Path.Combine(dayDirectory, fileName);

        var bounds = Screen.PrimaryScreen?.Bounds ?? new Rectangle(0, 0, 1, 1);
        using var bitmap = new Bitmap(bounds.Width, bounds.Height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(bounds.Location, Point.Empty, bounds.Size);
        bitmap.Save(path, ImageFormat.Jpeg);

        _database.InsertScreenshot(new ScreenshotRecord(
            now,
            path,
            activeWindow.ProcessName,
            activeWindow.WindowTitle,
            activeWindow.IsIdle));
    }
}
