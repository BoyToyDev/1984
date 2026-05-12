using ProductivityTracker.Data;
using ProductivityTracker.Reports;
using ProductivityTracker.Tracking;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class MainForm : Form
{
    private AppSettings _settings;
    private readonly TrackerDatabase _database;
    private readonly BrowserActivityReceiver _browserReceiver;
    private readonly HtmlReportGenerator _reportGenerator;
    private readonly Action<AppSettings>? _onSettingsChanged;
    private TabControl? _tabs;

    public MainForm(AppSettings settings, TrackerDatabase database, BrowserActivityReceiver browserReceiver, HtmlReportGenerator reportGenerator, Action<AppSettings>? onSettingsChanged = null)
    {
        _settings = settings;
        _database = database;
        _browserReceiver = browserReceiver;
        _reportGenerator = reportGenerator;
        _onSettingsChanged = onSettingsChanged;

        Text = settings.ProductName;
        StartPosition = FormStartPosition.CenterScreen;
        Size = new Size(1100, 700);

        _tabs = new TabControl { Dock = DockStyle.Fill };
        _tabs.TabPages.Add(BuildDashboardTab());
        _tabs.TabPages.Add(BuildProcessHistoryTab());
        _tabs.TabPages.Add(BuildWebHistoryTab());
        _tabs.TabPages.Add(BuildScreenshotsTab());
        _tabs.TabPages.Add(BuildReportsTab());
        _tabs.TabPages.Add(BuildSettingsTab());
        Controls.Add(_tabs);
    }

    private TabPage BuildDashboardTab()
    {
        var loc = _settings.Locale;
        var page = new TabPage(Loc.Get("dashboard", loc));
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            Padding = new Padding(16),
            AutoScroll = true
        };

        panel.Controls.Add(CreateDashboardCard(Loc.Get("status", loc), BuildDashboardText(), null));
        panel.Controls.Add(CreateDashboardCard(Loc.Get("processes", loc), Loc.Get("open_process_history", loc), 1));
        panel.Controls.Add(CreateDashboardCard(Loc.Get("web", loc), $"{Loc.Get("open_web_history", loc)} {Loc.Get("plugin_status", loc)}: {FormatBrowserPluginStatus()}", 2));
        panel.Controls.Add(CreateDashboardCard(Loc.Get("screenshots", loc), $"{Loc.Get("open_screenshots", loc)} {Loc.Get("retention_days", loc)}: {_settings.ScreenshotRetentionDays:N0} days.", 3));

        if (_browserReceiver.LastPluginSeenAt is null)
        {
            var installButton = new Button
            {
                Text = Loc.Get("install_plugin", loc),
                Width = 300,
                Height = 40
            };
            installButton.Click += (_, _) =>
            {
                var extDir = Path.Combine(AppContext.BaseDirectory, "browser-extension");
                if (!Directory.Exists(extDir))
                {
                    extDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "browser-extension"));
                }
                if (Directory.Exists(extDir))
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(extDir) { UseShellExecute = true });
                }
                MessageBox.Show(
                    Loc.Get("plugin_install_instructions", loc),
                    _settings.ProductName,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            };
            panel.Controls.Add(installButton);
        }

        page.Controls.Add(panel);
        return page;
    }

    private Button CreateDashboardCard(string title, string body, int? tabIndex)
    {
        var button = new Button
        {
            Text = $"{title}{Environment.NewLine}{body}",
            Width = 980,
            Height = 96,
            TextAlign = ContentAlignment.MiddleLeft
        };

        if (tabIndex is not null)
        {
            button.Click += (_, _) =>
            {
                if (_tabs is not null && tabIndex.Value < _tabs.TabPages.Count)
                {
                    _tabs.SelectedIndex = tabIndex.Value;
                }
            };
        }

        return button;
    }

    private string BuildDashboardText()
    {
        var loc = _settings.Locale;
        var topProcess = _database.Query("""
            SELECT process_name, MIN(started_at) AS first_started_at, MAX(ended_at) AS last_ended_at, SUM(duration_seconds) AS total_seconds
            FROM app_activity
            WHERE started_at >= $today
            GROUP BY process_name
            ORDER BY total_seconds DESC
            LIMIT 1;
            """, new Dictionary<string, object?> { ["$today"] = DateTime.Today.ToString("O") }).FirstOrDefault();

        var status = $"{Loc.Get("user", loc)}: {Environment.UserName}{Environment.NewLine}" +
                   $"{Loc.Get("data_folder", loc)}: {_settings.DataDirectory}{Environment.NewLine}" +
                   $"{Loc.Get("database", loc)}: {_settings.DatabasePath}{Environment.NewLine}" +
                   $"{Loc.Get("screenshot_folder", loc)}: {_settings.ScreenshotDirectory}{Environment.NewLine}" +
                   $"{Loc.Get("screenshot_interval_min", loc)}: {_settings.ScreenshotInterval.TotalMinutes:N0}{Environment.NewLine}" +
                   $"{Loc.Get("screenshot_retention_days", loc)}: {_settings.ScreenshotRetentionDays:N0}{Environment.NewLine}" +
                   $"Browser receiver port: {_settings.BrowserReceiverPort}{Environment.NewLine}" +
                   $"{Loc.Get("plugin_status", loc)}: {FormatBrowserPluginStatus()}{Environment.NewLine}";

        if (topProcess is null)
        {
            return status + $"{Environment.NewLine}{Loc.Get("no_activity", loc)}";
        }

        var processName = Convert.ToString(topProcess["process_name"]) ?? "unknown";
        var totalSeconds = Convert.ToDouble(topProcess["total_seconds"] ?? 0);
        var firstStarted = Convert.ToString(topProcess["first_started_at"]) ?? "unknown";
        var lastEnded = Convert.ToString(topProcess["last_ended_at"]) ?? "unknown";
        return status + $"{Environment.NewLine}{Loc.Fmt("was_opened_at", loc, processName, firstStarted, lastEnded, TimeSpan.FromSeconds(totalSeconds).ToString("hh\\:mm\\:ss"))}";
    }

    private TabPage BuildProcessHistoryTab()
    {
        var loc = _settings.Locale;
        var page = new TabPage(Loc.Get("processes", loc));
        var grid = CreateGrid();
        var filter = CreateHistoryFilterPanel(grid, LoadProcessHistory, includeSourceFilter: false);
        LoadProcessHistory(grid, filter.From.Value, filter.To.Value, filter.Search.Text, null);
        page.Controls.Add(grid);
        page.Controls.Add(filter.Panel);
        return page;
    }

    private TabPage BuildWebHistoryTab()
    {
        var loc = _settings.Locale;
        var page = new TabPage(Loc.Get("web", loc));
        var grid = CreateGrid();
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            var urlCol = Loc.Get("col_url", loc);
            var url = row.Cells[urlCol].Value?.ToString();
            if (!string.IsNullOrEmpty(url))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
            }
        };
        var filter = CreateHistoryFilterPanel(grid, LoadWebHistory, includeSourceFilter: true);
        LoadWebHistory(grid, filter.From.Value, filter.To.Value, filter.Search.Text, filter.Source.Text);
        page.Controls.Add(grid);
        page.Controls.Add(filter.Panel);
        return page;
    }

    private TabPage BuildScreenshotsTab()
    {
        var loc = _settings.Locale;
        var page = new TabPage(Loc.Get("screenshots", loc));
        var grid = CreateGrid();
        grid.CellDoubleClick += (_, e) =>
        {
            if (e.RowIndex < 0) return;
            var row = grid.Rows[e.RowIndex];
            var path = row.Cells["file_path"].Value?.ToString();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
            }
        };
        var filter = CreateHistoryFilterPanel(grid, LoadScreenshots, includeSourceFilter: false);
        LoadScreenshots(grid, filter.From.Value, filter.To.Value, filter.Search.Text, null);
        page.Controls.Add(grid);
        page.Controls.Add(filter.Panel);
        return page;
    }

    private HistoryFilterControls CreateHistoryFilterPanel(
        DataGridView grid,
        Action<DataGridView, DateTime, DateTime, string, string?> reload,
        bool includeSourceFilter)
    {
        var loc = _settings.Locale;
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 44,
            Padding = new Padding(8),
            FlowDirection = FlowDirection.LeftToRight
        };

        var from = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
        var to = new DateTimePicker { Format = DateTimePickerFormat.Short, Width = 110, Value = DateTime.Today };
        var search = new TextBox { Width = 220, PlaceholderText = Loc.Get("search", loc) };
        var source = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
        source.Items.AddRange(new object[] { Loc.Get("all", loc), Loc.Get("plugin", loc), Loc.Get("fallback_window_title", loc) });
        source.SelectedIndex = 0;

        var refresh = new Button { Text = Loc.Get("refresh", loc), Width = 90 };
        refresh.Click += (_, _) => reload(grid, from.Value.Date, to.Value.Date, search.Text, includeSourceFilter ? source.Text : null);

        panel.Controls.Add(new Label { Text = Loc.Get("from", loc), AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        panel.Controls.Add(from);
        panel.Controls.Add(new Label { Text = Loc.Get("to", loc), AutoSize = true, Padding = new Padding(0, 7, 0, 0) });
        panel.Controls.Add(to);
        panel.Controls.Add(search);
        if (includeSourceFilter)
        {
            panel.Controls.Add(source);
        }
        panel.Controls.Add(refresh);

        return new HistoryFilterControls(panel, from, to, search, source);
    }

    private void LoadProcessHistory(DataGridView grid, DateTime from, DateTime to, string search, string? source)
    {
        var loc = _settings.Locale;
        var parameters = CreateDateParameters(from, to);
        parameters["$search"] = $"%{search}%";
        var table = ToDataTable(_database.Query("""
            SELECT windows_user, process_name, window_title, started_at, duration_seconds,
                   SUM(duration_seconds) OVER (PARTITION BY process_name, date(started_at)) AS daily_total_seconds
            FROM app_activity
            WHERE started_at >= $from AND started_at < $to
              AND ($search = '%%' OR process_name LIKE $search OR window_title LIKE $search)
            ORDER BY started_at DESC
            LIMIT 1000;
            """, parameters));
        RenameColumns(table, loc, new() {
            ["windows_user"] = "col_user",
            ["process_name"] = "col_process_name",
            ["window_title"] = "col_window_title",
            ["started_at"] = "col_started_at",
            ["duration_seconds"] = "col_duration",
            ["daily_total_seconds"] = "col_daily_total"
        });
        grid.DataSource = table;
    }

    private void LoadWebHistory(DataGridView grid, DateTime from, DateTime to, string search, string? source)
    {
        var loc = _settings.Locale;
        var parameters = CreateDateParameters(from, to);
        parameters["$search"] = $"%{search}%";
        var sourceValue = source == Loc.Get("all", loc) ? string.Empty
            : source == Loc.Get("plugin", loc) ? "plugin"
            : source == Loc.Get("fallback_window_title", loc) ? "fallback_window_title"
            : source ?? string.Empty;
        parameters["$source"] = sourceValue;
        var table = ToDataTable(_database.Query("""
            SELECT windows_user, browser, title, domain, url, started_at, duration_seconds,
                   SUM(duration_seconds) OVER (PARTITION BY domain, title, date(started_at)) AS daily_total_seconds
            FROM browser_activity
            WHERE started_at >= $from AND started_at < $to
              AND ($source = '' OR source = $source)
              AND ($search = '%%' OR browser LIKE $search OR domain LIKE $search OR title LIKE $search OR url LIKE $search)
            ORDER BY started_at DESC
            LIMIT 1000;
            """, parameters));
        RenameColumns(table, loc, new() {
            ["windows_user"] = "col_user",
            ["browser"] = "col_browser",
            ["title"] = "col_window_title",
            ["domain"] = "col_domain",
            ["url"] = "col_url",
            ["started_at"] = "col_started_at",
            ["duration_seconds"] = "col_duration",
            ["daily_total_seconds"] = "col_daily_total"
        });
        grid.DataSource = table;
    }

    private void LoadScreenshots(DataGridView grid, DateTime from, DateTime to, string search, string? source)
    {
        var loc = _settings.Locale;
        var parameters = CreateDateParameters(from, to);
        parameters["$search"] = $"%{search}%";
        var table = ToDataTable(_database.Query("""
            SELECT windows_user, active_process, active_window_title, captured_at, file_path
            FROM screenshots
            WHERE captured_at >= $from AND captured_at < $to
              AND ($search = '%%' OR active_process LIKE $search OR active_window_title LIKE $search)
            ORDER BY captured_at DESC
            LIMIT 1000;
            """, parameters));
        RenameColumns(table, loc, new() {
            ["windows_user"] = "col_user",
            ["active_process"] = "col_process_name",
            ["active_window_title"] = "col_window_title",
            ["captured_at"] = "col_captured_at"
        });
        grid.DataSource = table;
        if (grid.Columns["file_path"] is DataGridViewColumn col)
        {
            col.Visible = false;
        }
    }

    private static void RenameColumns(DataTable table, string locale, Dictionary<string, string> mapping)
    {
        foreach (var (internalName, locKey) in mapping)
        {
            if (table.Columns[internalName] is DataColumn col)
            {
                col.ColumnName = Loc.Get(locKey, locale);
            }
        }
    }

    private static Dictionary<string, object?> CreateDateParameters(DateTime from, DateTime to)
    {
        return new Dictionary<string, object?>
        {
            ["$from"] = from.ToString("O"),
            ["$to"] = to.AddDays(1).ToString("O")
        };
    }

    private TabPage BuildReportsTab()
    {
        var loc = _settings.Locale;
        var page = new TabPage(Loc.Get("reports", loc));
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
            Text = Loc.Get("generate_report", loc),
            Width = 220
        };
        generateButton.Click += (_, _) =>
        {
            var day = DateOnly.FromDateTime(datePicker.Value.Date);
            _reportGenerator.OpenDailyReport(day);
        };

        var openFolderButton = new Button
        {
            Text = Loc.Get("open_reports_folder", loc),
            Width = 220
        };
        openFolderButton.Click += (_, _) =>
        {
            Directory.CreateDirectory(_settings.ReportDirectory);
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(_settings.ReportDirectory) { UseShellExecute = true });
        };

        panel.Controls.Add(new Label { Text = Loc.Get("report_date", loc) + ":", AutoSize = true });
        panel.Controls.Add(datePicker);
        panel.Controls.Add(generateButton);
        panel.Controls.Add(openFolderButton);
        page.Controls.Add(panel);
        return page;
    }

    private TabPage BuildSettingsTab()
    {
        var loc = _settings.Locale;
        var page = new TabPage(Loc.Get("settings", loc));
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 17,
            Padding = new Padding(16),
            AutoScroll = true
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120));

        var dbPathBox = new TextBox { Text = _settings.DatabasePath, Dock = DockStyle.Fill };
        var screenshotFolderBox = new TextBox { Text = _settings.ScreenshotDirectory, Dock = DockStyle.Fill };
        var screenshotIntervalBox = new NumericUpDown { Minimum = 1, Maximum = 1440, Value = (decimal)Math.Max(1, _settings.ScreenshotInterval.TotalMinutes), Dock = DockStyle.Left };
        var screenshotRetentionBox = new NumericUpDown { Minimum = 1, Maximum = 3650, Value = _settings.ScreenshotRetentionDays, Dock = DockStyle.Left };
        var idleThresholdBox = new NumericUpDown { Minimum = 1, Maximum = 1440, Value = (decimal)Math.Max(1, _settings.IdleThreshold.TotalMinutes), Dock = DockStyle.Left };
        var retentionBox = new NumericUpDown { Minimum = 1, Maximum = 3650, Value = _settings.RetentionDays, Dock = DockStyle.Left };
        var heartbeatBox = new NumericUpDown { Minimum = 10, Maximum = 3600, Value = _settings.BrowserPluginHeartbeatIntervalSeconds, Dock = DockStyle.Left };
        var quietModeBox = new CheckBox { Checked = _settings.QuietMode, Text = Loc.Get("quiet_operation", loc), AutoSize = true };
        var notificationsBox = new CheckBox { Checked = _settings.ShowNotifications, Text = Loc.Get("notifications", loc), AutoSize = true };
        var languageBox = new ComboBox { Width = 160, DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Left };
        languageBox.Items.AddRange(new object[] { "English", "Русский" });
        languageBox.SelectedIndex = _settings.Locale == "ru" ? 1 : 0;
        var autoStartBox = new CheckBox { Checked = _settings.AutoStart, Text = Loc.Get("autostart", loc), AutoSize = true };

        var row = 0;
        AddSettingRow(panel, row++, Loc.Get("database_path", loc), dbPathBox, CreateBrowseFileButton(dbPathBox, "SQLite databases|*.db|All files|*.*"));
        AddSettingRow(panel, row++, Loc.Get("screenshot_folder", loc), screenshotFolderBox, CreateBrowseButton(screenshotFolderBox));
        AddSettingRow(panel, row++, Loc.Get("screenshot_interval_min", loc), screenshotIntervalBox);
        AddSettingRow(panel, row++, Loc.Get("screenshot_retention_days", loc), screenshotRetentionBox);
        AddSettingRow(panel, row++, Loc.Get("idle_threshold_min", loc), idleThresholdBox);
        AddSettingRow(panel, row++, Loc.Get("activity_retention_days", loc), retentionBox);
        AddSettingRow(panel, row++, Loc.Get("plugin_heartbeat_sec", loc), heartbeatBox);
        AddSettingRow(panel, row++, Loc.Get("language", loc), languageBox);
        AddSettingRow(panel, row++, string.Empty, autoStartBox);
        AddSettingRow(panel, row++, string.Empty, quietModeBox);
        AddSettingRow(panel, row++, string.Empty, notificationsBox);

        var saveButton = new Button { Text = Loc.Get("save_settings", loc), Width = 160 };
        saveButton.Click += (_, _) =>
        {
            var newLocale = languageBox.SelectedIndex == 1 ? "ru" : "en";
            _settings = _settings.With(
                databasePath: dbPathBox.Text,
                screenshotDirectory: screenshotFolderBox.Text,
                screenshotInterval: TimeSpan.FromMinutes((double)screenshotIntervalBox.Value),
                idleThreshold: TimeSpan.FromMinutes((double)idleThresholdBox.Value),
                retentionDays: (int)retentionBox.Value,
                screenshotRetentionDays: (int)screenshotRetentionBox.Value,
                quietMode: quietModeBox.Checked,
                showNotifications: notificationsBox.Checked,
                browserPluginHeartbeatIntervalSeconds: (int)heartbeatBox.Value,
                locale: newLocale,
                autoStart: autoStartBox.Checked);
            _settings.Save();
            Directory.CreateDirectory(_settings.ScreenshotDirectory);
            _onSettingsChanged?.Invoke(_settings);
            MessageBox.Show(Loc.Get("settings_saved", newLocale), _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        var changePasswordButton = new Button { Text = Loc.Get("change_password", loc), Width = 160 };
        changePasswordButton.Click += (_, _) => ChangePassword();

        var openDataButton = new Button { Text = Loc.Get("open_data_folder", loc), Width = 160 };
        openDataButton.Click += (_, _) => OpenFolder(_settings.DataDirectory);

        var cleanupButton = new Button { Text = Loc.Get("run_cleanup_now", loc), Width = 160 };
        cleanupButton.Click += (_, _) =>
        {
            _onSettingsChanged?.Invoke(_settings);
            MessageBox.Show(Loc.Get("cleanup_completed", _settings.Locale), _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        };

        panel.Controls.Add(saveButton, 1, row++);
        panel.Controls.Add(changePasswordButton, 1, row++);
        panel.Controls.Add(cleanupButton, 1, row++);
        panel.Controls.Add(openDataButton, 1, row++);
        page.Controls.Add(panel);
        return page;
    }

    private Button CreateBrowseButton(TextBox target)
    {
        var button = new Button { Text = Loc.Get("browse", _settings.Locale), Width = 90 };
        button.Click += (_, _) =>
        {
            using var dialog = new FolderBrowserDialog { SelectedPath = Directory.Exists(target.Text) ? target.Text : _settings.DataDirectory };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                target.Text = dialog.SelectedPath;
            }
        };
        return button;
    }

    private Button CreateBrowseFileButton(TextBox target, string filter)
    {
        var button = new Button { Text = Loc.Get("browse", _settings.Locale), Width = 90 };
        button.Click += (_, _) =>
        {
            using var dialog = new OpenFileDialog
            {
                Filter = filter,
                FileName = target.Text,
                CheckFileExists = false
            };
            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                target.Text = dialog.FileName;
            }
        };
        return button;
    }

    private static void AddSettingRow(TableLayoutPanel panel, int row, string label, Control editor, Control? action = null)
    {
        panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        panel.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        panel.Controls.Add(editor, 1, row);
        if (action is not null)
        {
            panel.Controls.Add(action, 2, row);
        }
    }

    private void ChangePassword()
    {
        var loc = _settings.Locale;
        using var dialog = new ChangePasswordForm(_settings.ProductName, loc);
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var verifier = new ExitPasswordVerifier(_settings);
        if (!verifier.Verify(dialog.CurrentPassword))
        {
            MessageBox.Show(Loc.Get("current_password_invalid", loc), _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _settings = _settings.WithAccessPassword(dialog.NewPassword);
        _settings.Save();
        MessageBox.Show(Loc.Get("password_changed", loc), _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private static void OpenFolder(string path)
    {
        Directory.CreateDirectory(path);
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(path) { UseShellExecute = true });
    }

    private string FormatBrowserPluginStatus()
    {
        var loc = _settings.Locale;
        var lastSeen = _browserReceiver.LastPluginSeenAt;
        if (lastSeen is null)
        {
            return Loc.Get("limited_mode", loc);
        }

        var age = DateTimeOffset.Now - lastSeen.Value;
        var activeWindow = TimeSpan.FromSeconds(Math.Max(30, _settings.BrowserPluginHeartbeatIntervalSeconds * 3));
        return age <= activeWindow
            ? Loc.Fmt("active_last_event", loc, age.TotalSeconds.ToString("N0"))
            : Loc.Fmt("limited_last_event", loc, $"{lastSeen.Value:yyyy-MM-dd HH:mm:ss}");
    }

    private static DataTable ToDataTable(IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var table = new DataTable();
        if (rows.Count == 0)
        {
            return table;
        }

        foreach (var key in rows[0].Keys)
        {
            table.Columns.Add(key);
        }

        foreach (var row in rows)
        {
            var values = new object?[row.Count];
            var i = 0;
            foreach (var key in row.Keys)
            {
                values[i++] = row[key];
            }
            table.Rows.Add(values);
        }

        return table;
    }

    private static DataGridView CreateGrid()
    {
        return new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            AllowUserToOrderColumns = true,
            AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle { BackColor = Color.FromArgb(245, 245, 245) }
        };
    }

    private sealed record HistoryFilterControls(
        FlowLayoutPanel Panel,
        DateTimePicker From,
        DateTimePicker To,
        TextBox Search,
        ComboBox Source);
}
