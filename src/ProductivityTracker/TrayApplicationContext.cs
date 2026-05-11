using Microsoft.Win32;
using ProductivityTracker.Data;
using ProductivityTracker.Reports;
using ProductivityTracker.Tracking;
using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly TrackerDatabase _database;
    private readonly ActivityTracker _activityTracker;
    private readonly BrowserActivityReceiver _browserReceiver;
    private readonly HtmlReportGenerator _reportGenerator;
    private readonly ExitPasswordVerifier _passwordVerifier;
    private readonly NotifyIcon _notifyIcon;

    public TrayApplicationContext(
        AppSettings settings,
        TrackerDatabase database,
        ActivityTracker activityTracker,
        BrowserActivityReceiver browserReceiver,
        HtmlReportGenerator reportGenerator)
    {
        _settings = settings;
        _database = database;
        _activityTracker = activityTracker;
        _browserReceiver = browserReceiver;
        _reportGenerator = reportGenerator;
        _passwordVerifier = new ExitPasswordVerifier(settings);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = _settings.ProductName,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };

        _notifyIcon.DoubleClick += (_, _) => RequestOpenApplication();
        EnableAutoStart();
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Open application", null, (_, _) => RequestOpenApplication());
        menu.Items.Add("Close 1984", null, (_, _) => RequestExit());
        return menu;
    }

    private void EnableAutoStart()
    {
        var exePath = Application.ExecutablePath;
        using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
        key?.SetValue(_settings.ProductName, $"\"{exePath}\"");
    }

    private void RequestOpenApplication()
    {
        if (!RequestPassword("open the application"))
        {
            return;
        }

        using var form = new MainForm(_settings, _database);
        form.ShowDialog();
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

        using var dialog = new ExitPasswordDialog(_settings.ProductName, actionName);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return false;
        }

        if (_passwordVerifier.Verify(dialog.Password))
        {
            return true;
        }

        MessageBox.Show("Invalid password.", _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return false;
    }

    protected override void ExitThreadCore()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _activityTracker.Dispose();
        _browserReceiver.Dispose();
        _reportGenerator.Dispose();
        base.ExitThreadCore();
    }
}
