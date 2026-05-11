using ProductivityTracker.Data;
using ProductivityTracker.Reports;
using ProductivityTracker.Tracking;
using ProductivityTracker.Windows;
using System.Windows.Forms;

namespace ProductivityTracker;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = AppSettings.LoadDefault();
        Directory.CreateDirectory(settings.DataDirectory);
        Directory.CreateDirectory(settings.ScreenshotDirectory);
        Directory.CreateDirectory(settings.ReportDirectory);

        using var database = new TrackerDatabase(settings.DatabasePath);
        database.Initialize();

        using var activityTracker = new ActivityTracker(database, settings);
        using var browserReceiver = new BrowserActivityReceiver(database, settings);
        using var reportGenerator = new HtmlReportGenerator(database, settings);
        using var trayApp = new TrayApplicationContext(settings, activityTracker, browserReceiver, reportGenerator);

        activityTracker.Start();
        browserReceiver.Start();

        Application.Run(trayApp);
    }
}
