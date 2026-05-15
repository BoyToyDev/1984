using Microsoft.Win32;
using ProductivityTracker.Data;
using ProductivityTracker.Reports;
using ProductivityTracker.Tracking;
using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private AppSettings _settings;
    private readonly TrackerDatabase _database;
    private readonly ActivityTracker _activityTracker;
    private readonly BrowserActivityReceiver _browserReceiver;
    private readonly RetentionCleanupService _retentionCleanup;
    private readonly HtmlReportGenerator _reportGenerator;
    private readonly ExitPasswordVerifier _passwordVerifier;
    private readonly NotifyIcon _notifyIcon;
    private System.Windows.Forms.Timer? _reportTimer;

    public TrayApplicationContext(
        AppSettings settings,
        TrackerDatabase database,
        ActivityTracker activityTracker,
        BrowserActivityReceiver browserReceiver,
        RetentionCleanupService retentionCleanup,
        HtmlReportGenerator reportGenerator)
    {
        _settings = settings;
        _database = database;
        _activityTracker = activityTracker;
        _browserReceiver = browserReceiver;
        _retentionCleanup = retentionCleanup;
        _reportGenerator = reportGenerator;
        _passwordVerifier = new ExitPasswordVerifier(settings);

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!,
            Text = _settings.ProductName,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => RequestOpenApplication();
        EnableAutoStart();
        StartAutoReport();
    }

    private ContextMenuStrip BuildMenu()
    {
        var loc = _settings.Locale;
        var menu = new ContextMenuStrip();
        menu.Items.Add(Loc.Get("open_application", loc), null, (_, _) => RequestOpenApplication());
        menu.Items.Add(Loc.Get("close_app", loc), null, (_, _) => RequestExit());
        return menu;
    }

    private void EnableAutoStart()
    {
        var exePath = Application.ExecutablePath;
        using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
        if (key is null)
        {
            return;
        }

        if (_settings.AutoStart)
        {
            key.SetValue(_settings.ProductName, $"\"{exePath}\"");
        }
        else
        {
            try { key.DeleteValue(_settings.ProductName, throwOnMissingValue: false); } catch { }
        }
    }

    private void RequestOpenApplication()
    {
        if (!RequestPassword("open the application"))
        {
            return;
        }

        using var form = new MainForm(_settings, _database, _browserReceiver, _reportGenerator, OnSettingsChanged);
        form.ShowDialog();
    }

    private void OnSettingsChanged(AppSettings newSettings)
    {
        _settings = newSettings;
        _activityTracker.ApplySettings(newSettings);
        _retentionCleanup.RunNow();
        EnableAutoStart();
        StartAutoReport();
    }

    private void StartAutoReport()
    {
        _reportTimer?.Stop();
        _reportTimer?.Dispose();
        _reportTimer = null;

        if (_settings.ReportAutoIntervalMinutes <= 0)
        {
            return;
        }

        _reportTimer = new System.Windows.Forms.Timer
        {
            Interval = (int)TimeSpan.FromMinutes(_settings.ReportAutoIntervalMinutes).TotalMilliseconds
        };
        _reportTimer.Tick += (_, _) =>
        {
            try
            {
                _reportGenerator.GenerateDailyReport(DateOnly.FromDateTime(DateTime.Today));
            }
            catch
            {
            }
        };
        _reportTimer.Start();
    }

    private void RequestExit()
    {
        if (RequestPassword("close 1984"))
        {
            ExitThread();
        }
    }

    private bool RequestPassword(string actionName)
    {
        if (!_passwordVerifier.IsPasswordRequired)
        {
            return true;
        }

        using var dialog = new ExitPasswordDialog(_settings.ProductName, actionName, _settings.Locale);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return false;
        }

        if (_passwordVerifier.Verify(dialog.Password))
        {
            return true;
        }

        MessageBox.Show(Loc.Get("invalid_password", _settings.Locale), _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _activityTracker.Dispose();
        _browserReceiver.Dispose();
        _retentionCleanup.Dispose();
        _reportGenerator.Dispose();
        base.ExitThreadCore();
    }
}
