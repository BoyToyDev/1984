using System.Diagnostics;
using System.Text;
using ProductivityTracker.Windows;

namespace ProductivityTracker.Tracking;

internal sealed class ActiveWindowReader
{
    private readonly AppSettings _settings;

    public ActiveWindowReader(AppSettings settings)
    {
        _settings = settings;
    }

    public ActiveWindowSnapshot Read()
    {
        var handle = NativeMethods.GetForegroundWindow();
        if (handle == IntPtr.Zero)
        {
            return new ActiveWindowSnapshot("unknown", null, string.Empty, IsIdle());
        }

        NativeMethods.GetWindowThreadProcessId(handle, out var processId);
        var title = GetWindowTitle(handle);

        try
        {
            using var process = Process.GetProcessById((int)processId);
            string? path = null;
            try
            {
                path = process.MainModule?.FileName;
            }
            catch
            {
                path = null;
            }

            return new ActiveWindowSnapshot(process.ProcessName, path, title, IsIdle());
        }
        catch
        {
            return new ActiveWindowSnapshot("unknown", null, title, IsIdle());
        }
    }

    private bool IsIdle() => IdleDetector.GetIdleTime() >= _settings.IdleThreshold;

    private static string GetWindowTitle(IntPtr handle)
    {
        var builder = new StringBuilder(512);
        _ = NativeMethods.GetWindowText(handle, builder, builder.Capacity);
        return builder.ToString();
    }
}
