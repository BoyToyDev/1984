using Microsoft.Win32;
using ProductivityTracker.Reports;
using ProductivityTracker.Tracking;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace ProductivityTracker;

internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly AppSettings _settings;
    private readonly ActivityTracker _activityTracker;
    private readonly BrowserActivityReceiver _browserReceiver;
    private readonly HtmlReportGenerator _reportGenerator;
    private readonly ExitPasswordVerifier _exitPasswordVerifier;
    private readonly NotifyIcon _notifyIcon;

    public TrayApplicationContext(
        AppSettings settings,
        ActivityTracker activityTracker,
        BrowserActivityReceiver browserReceiver,
        HtmlReportGenerator reportGenerator)
    {
        _settings = settings;
        _activityTracker = activityTracker;
        _browserReceiver = browserReceiver;
        _reportGenerator = reportGenerator;
        _exitPasswordVerifier = new ExitPasswordVerifier(settings);

        _notifyIcon = new NotifyIcon
        {
            Icon = SystemIcons.Application,
            Text = _settings.ProductName,
            Visible = true,
            ContextMenuStrip = BuildMenu()
        };
    }

    private ContextMenuStrip BuildMenu()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Generate today's report", null, (_, _) => _reportGenerator.OpenDailyReport(DateOnly.FromDateTime(DateTime.Now)));
        menu.Items.Add("Open data folder", null, (_, _) => Process.Start(new ProcessStartInfo(_settings.DataDirectory) { UseShellExecute = true }));
        menu.Items.Add("Enable auto-start", null, (_, _) => EnableAutoStart());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, (_, _) => RequestExit());
        return menu;
    }

    private void EnableAutoStart()
    {
        var exePath = Application.ExecutablePath;
        using var key = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run", writable: true);
        key?.SetValue(_settings.ProductName, $"\"{exePath}\"");
        MessageBox.Show("Auto-start enabled for current user.", _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RequestExit()
    {
        if (!_exitPasswordVerifier.IsPasswordRequired)
        {
            ExitThread();
            return;
        }

        using var dialog = new ExitPasswordDialog(_settings.ProductName);
        if (dialog.ShowDialog() != DialogResult.OK)
        {
            return;
        }

        if (_exitPasswordVerifier.Verify(dialog.Password))
        {
            ExitThread();
            return;
        }

        MessageBox.Show("Invalid exit password.", _settings.ProductName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
