using ProductivityTracker.Data;
using ProductivityTracker.Reports;
using ProductivityTracker.Tracking;
using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class MainForm : Form
{
    private readonly AppSettings _settings;
    private readonly TrackerDatabase _database;
    private readonly BrowserActivityReceiver _browserReceiver;
    private readonly HtmlReportGenerator _reportGenerator;

    public MainForm(AppSettings settings, TrackerDatabase database, BrowserActivityReceiver browserReceiver, HtmlReportGenerator reportGenerator)
    {
        _settings = settings;
        _database = database;
        _browserReceiver = browserReceiver;
        _reportGenerator = reportGenerator;

        Text = settings.ProductName;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1100, 700);

        var tabs = new TabControl { Dock = DockStyle.Fill };
        tabs.TabPages.Add(BuildDashboardTab());
        tabs.TabPages.Add(BuildProcessHistoryTab());
        tabs.TabPages.Add(BuildWebHistoryTab());
        tabs.TabPages.Add(BuildScreenshotsTab());
        tabs.TabPages.Add(BuildReportsTab());
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
                   $"Browser plugin status: {FormatBrowserPluginStatus()}"
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

    private TabPage BuildReportsTab()
    {
        var page = new TabPage("Reports");
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(16),
            AutoScroll = true
        };

        var datePicker = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Width = 160,
            Value = DateTime.Today
        };

        var generateButton = new Button
        {
            Text = "Generate and open report",
            Width = 220
        };
        generateButton.Click += (_, _) =>
        {
            var day = DateOnly.FromDateTime(datePicker.Value.Date);
            _reportGenerator.OpenDailyReport(day);
        };

        var openFolderButton = new Button
        {
            Text = "Open reports folder",
            Width = 220
        };
        openFolderButton.Click += (_, _) =>
        {
            Directory.CreateDirectory(_settings.ReportDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_settings.ReportDirectory) { UseShellExecute = true });
        };

        panel.Controls.Add(new Label { Text = "Report date:", AutoSize = true });
        panel.Controls.Add(datePicker);
        panel.Controls.Add(generateButton);
        panel.Controls.Add(openFolderButton);
        page.Controls.Add(panel);
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

    private string FormatBrowserPluginStatus()
    {
        var lastSeen = _browserReceiver.LastPluginSeenAt;
        if (lastSeen is null)
        {
            return "limited mode, plugin not detected yet";
        }

        var age = DateTimeOffset.Now - lastSeen.Value;
        var activeWindow = TimeSpan.FromSeconds(Math.Max(30, _settings.BrowserPluginHeartbeatIntervalSeconds * 3));
        return age <= activeWindow
            ? $"active, last event {age.TotalSeconds:N0} seconds ago"
            : $"limited mode, last event {lastSeen.Value:yyyy-MM-dd HH:mm:ss}";
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
