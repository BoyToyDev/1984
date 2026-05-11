using System.Runtime.InteropServices;
using ProductivityTracker.Windows;

namespace ProductivityTracker.Tracking;

internal static class IdleDetector
{
    public static TimeSpan GetIdleTime()
    {
        var info = new NativeMethods.LastInputInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.LastInputInfo>()
        };

        if (!NativeMethods.GetLastInputInfo(ref info))
        {
            return TimeSpan.Zero;
        }

        var elapsedMilliseconds = Environment.TickCount64 - info.Time;
        return TimeSpan.FromMilliseconds(Math.Max(0, elapsedMilliseconds));
    }
}
