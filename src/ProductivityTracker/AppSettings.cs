using System.Text.Json;

namespace ProductivityTracker;

public sealed class AppSettings
{
    public string ProductName { get; init; } = "1984";
    public string CompanyName { get; init; } = "Organization";
    public string InstallDirectory { get; init; } = AppContext.BaseDirectory;
    public string ConfigPath => Path.Combine(InstallDirectory, "1984.config.json");
    public string DataDirectory { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "1984");
    public string DatabasePath => Path.Combine(DataDirectory, "tracker.db");
    public string ScreenshotDirectory => Path.Combine(DataDirectory, "screenshots");
    public string ReportDirectory => Path.Combine(DataDirectory, "reports");
    public TimeSpan ActiveWindowPollInterval { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan ScreenshotInterval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdleThreshold { get; init; } = TimeSpan.FromMinutes(3);
    public int BrowserReceiverPort { get; init; } = 39877;
    public int RetentionDays { get; init; } = 90;
    public bool AllowExitWithoutPassword { get; init; } = true;
    public string? ExitPasswordHashBase64 { get; init; }
    public string? ExitPasswordSaltBase64 { get; init; }
    public int ExitPasswordIterations { get; init; } = 100_000;

    public static AppSettings LoadDefault()
    {
        var defaults = new AppSettings();
        if (!File.Exists(defaults.ConfigPath))
        {
            return defaults;
        }

        var config = JsonSerializer.Deserialize<InstallConfig>(
            File.ReadAllText(defaults.ConfigPath),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (config is null)
        {
            return defaults;
        }

        return new AppSettings
        {
            ProductName = config.ProductName ?? defaults.ProductName,
            CompanyName = config.CompanyName ?? defaults.CompanyName,
            InstallDirectory = defaults.InstallDirectory,
            DataDirectory = ExpandPath(config.DataDirectory) ?? defaults.DataDirectory,
            ActiveWindowPollInterval = TimeSpan.FromSeconds(config.ActiveWindowPollIntervalSeconds ?? defaults.ActiveWindowPollInterval.TotalSeconds),
            ScreenshotInterval = TimeSpan.FromSeconds(config.ScreenshotIntervalSeconds ?? defaults.ScreenshotInterval.TotalSeconds),
            IdleThreshold = TimeSpan.FromSeconds(config.IdleThresholdSeconds ?? defaults.IdleThreshold.TotalSeconds),
            BrowserReceiverPort = config.BrowserReceiverPort ?? defaults.BrowserReceiverPort,
            RetentionDays = config.RetentionDays ?? defaults.RetentionDays,
            AllowExitWithoutPassword = config.AllowExitWithoutPassword ?? defaults.AllowExitWithoutPassword,
            ExitPasswordHashBase64 = config.ExitPasswordHashBase64,
            ExitPasswordSaltBase64 = config.ExitPasswordSaltBase64,
            ExitPasswordIterations = config.ExitPasswordIterations ?? defaults.ExitPasswordIterations
        };
    }

    private static string? ExpandPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        return Environment.ExpandEnvironmentVariables(path);
    }

    private sealed class InstallConfig
    {
        public string? ProductName { get; set; }
        public string? CompanyName { get; set; }
        public string? DataDirectory { get; set; }
        public double? ActiveWindowPollIntervalSeconds { get; set; }
        public double? ScreenshotIntervalSeconds { get; set; }
        public double? IdleThresholdSeconds { get; set; }
        public int? BrowserReceiverPort { get; set; }
        public int? RetentionDays { get; set; }
        public bool? AllowExitWithoutPassword { get; set; }
        public string? ExitPasswordHashBase64 { get; set; }
        public string? ExitPasswordSaltBase64 { get; set; }
        public int? ExitPasswordIterations { get; set; }
    }
}
