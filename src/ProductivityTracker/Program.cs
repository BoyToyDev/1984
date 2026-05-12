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
        if (settings.IsFirstRun)
        {
            using var setupForm = new FirstRunSetupForm(settings.ProductName, settings.Locale);
            if (setupForm.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            settings = settings.WithAccessPassword(setupForm.Password);
            settings.Save();
        }

        Directory.CreateDirectory(settings.DataDirectory);
        Directory.CreateDirectory(settings.ScreenshotDirectory);
        Directory.CreateDirectory(settings.ReportDirectory);

        TrackerDatabase database;
        try
        {
            var dbDir = Path.GetDirectoryName(settings.DatabasePath);
            if (!string.IsNullOrEmpty(dbDir))
            {
                Directory.CreateDirectory(dbDir);
            }

            database = new TrackerDatabase(settings.DatabasePath);
            database.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"{Loc.Get("db_unavailable", settings.Locale)}\n\n{ex.Message}",
                Loc.Get("db_error_title", settings.Locale),
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        using var activityTracker = new ActivityTracker(database, settings);
        using var browserReceiver = new BrowserActivityReceiver(database, settings);
        using var retentionCleanup = new RetentionCleanupService(database, settings);
        using var reportGenerator = new HtmlReportGenerator(database, settings);
        using var trayApp = new TrayApplicationContext(settings, database, activityTracker, browserReceiver, retentionCleanup, reportGenerator);

        activityTracker.Start();
        browserReceiver.Start();
        retentionCleanup.Start();

        Application.Run(trayApp);
    }
}
