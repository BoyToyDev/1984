using ProductivityTracker.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly TrackerDatabase _database;

    public MainForm(AppSettings settings, TrackerDatabase database)
    {
        _settings = settings;
        _database = database;

        Text = settings.ProductName;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1100, 700);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildDashboardTab());
        tabs.TabPages.Add(BuildProcessHistoryTab());
        tabs.TabPages.Add(BuildWebHistoryTab());
        tabs.TabPages.Add(BuildScreenshotsTab());
        tabs.TabPages.Add(BuildSettingsTab());
        Controls.Add(tabs);
    }

    private TabPage BuildDashboardTab()
    {
        var page = new TabPage("Dashboard");
        var text = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = $"User: {Environment.UserName}{Environment.NewLine}" +
                   $"Data folder: {_settings.DataDirectory}{Environment.NewLine}" +
                   $"Database: {_settings.DatabasePath}{Environment.NewLine}" +
                   $"Screenshots: {_settings.ScreenshotDirectory}{Environment.NewLine}" +
                   $"Screenshot interval: {_settings.ScreenshotInterval.TotalMinutes:N0} minutes{Environment.NewLine}" +
                   $"Browser receiver port: {_settings.BrowserReceiverPort}{Environment.NewLine}" +
                   $"Browser plugin status: limited until heartbeat support is implemented"
        };
        page.Controls.Add(text);
        return page;
    }

    private TabPage BuildProcessHistoryTab()
    {
        var page = new TabPage("Processes");
        var grid = CreateGrid();
        grid.DataSource = _database.Query("""
            SELECT started_at, ended_at, duration_seconds, process_name, executable_path, window_title, is_idle
            FROM app_activity
            ORDER BY started_at DESC
            LIMIT 1000;
            """);
        page.Controls.Add(grid);
        return page;
    }

    private TabPage BuildWebHistoryTab()
    {
        var page = new TabPage("Web");
        var grid = CreateGrid();
        grid.DataSource = _database.Query("""
            SELECT started_at, ended_at, duration_seconds, browser, title, url
            FROM browser_activity
            ORDER BY started_at DESC
            LIMIT 1000;
            """);
        page.Controls.Add(grid);
        return page;
    }

    private TabPage BuildScreenshotsTab()
    {
        var page = new TabPage("Screenshots");
        var grid = CreateGrid();
        grid.DataSource = _database.Query("""
            SELECT captured_at, file_path, active_process, active_window_title, is_idle
            FROM screenshots
            ORDER BY captured_at DESC
            LIMIT 1000;
            """);
        page.Controls.Add(grid);
        return page;
    }

    private TabPage BuildSettingsTab()
    {
        var page = new TabPage("Settings");
        var text = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ReadOnly = true,
            ScrollBars = ScrollBars.Vertical,
            Text = "Editable settings UI is planned next." + Environment.NewLine + Environment.NewLine +
                   $"Screenshot folder: {_settings.ScreenshotDirectory}{Environment.NewLine}" +
                   $"Screenshot interval minutes: {_settings.ScreenshotInterval.TotalMinutes:N0}{Environment.NewLine}" +
                   $"Idle threshold minutes: {_settings.IdleThreshold.TotalMinutes:N0}{Environment.NewLine}" +
                   $"Retention days: {_settings.RetentionDays}{Environment.NewLine}" +
                   "Planned: change password, choose screenshot folder, browser plugin status, exclusions, retention cleanup."
        };
        page.Controls.Add(text);
        return page;
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
        };
    }
}
