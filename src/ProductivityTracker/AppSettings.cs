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
    public string ScreenshotDirectory { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "1984", "screenshots");
    public string ReportDirectory => Path.Combine(DataDirectory, "reports");
    public TimeSpan ActiveWindowPollInterval { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan ScreenshotInterval { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan IdleThreshold { get; init; } = TimeSpan.FromMinutes(3);
    public int BrowserReceiverPort { get; init; } = 39877;
    public int RetentionDays { get; init; } = 90;
    public int ScreenshotRetentionDays { get; init; } = 30;
    public bool QuietMode { get; init; } = true;
    public bool ShowNotifications { get; init; } = false;
    public int BrowserPluginHeartbeatIntervalSeconds { get; init; } = 60;
    public bool AllowExitWithoutPassword { get; init; } = true;
    public string? ExitPasswordHashBase64 { get; init; }
    public string? ExitPasswordSaltBase64 { get; init; }
    public int ExitPasswordIterations { get; init; } = 100_000;
    public bool IsFirstRun => string.IsNullOrWhiteSpace(ExitPasswordHashBase64) || string.IsNullOrWhiteSpace(ExitPasswordSaltBase64);

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
            ScreenshotDirectory = ExpandPath(config.ScreenshotDirectory) ?? Path.Combine(ExpandPath(config.DataDirectory) ?? defaults.DataDirectory, "screenshots"),
            ActiveWindowPollInterval = TimeSpan.FromSeconds(config.ActiveWindowPollIntervalSeconds ?? defaults.ActiveWindowPollInterval.TotalSeconds),
            ScreenshotInterval = TimeSpan.FromSeconds(config.ScreenshotIntervalSeconds ?? defaults.ScreenshotInterval.TotalSeconds),
            IdleThreshold = TimeSpan.FromSeconds(config.IdleThresholdSeconds ?? defaults.IdleThreshold.TotalSeconds),
            BrowserReceiverPort = config.BrowserReceiverPort ?? defaults.BrowserReceiverPort,
            RetentionDays = config.RetentionDays ?? defaults.RetentionDays,
            ScreenshotRetentionDays = config.ScreenshotRetentionDays ?? defaults.ScreenshotRetentionDays,
            QuietMode = config.QuietMode ?? defaults.QuietMode,
            ShowNotifications = config.ShowNotifications ?? defaults.ShowNotifications,
            BrowserPluginHeartbeatIntervalSeconds = config.BrowserPluginHeartbeatIntervalSeconds ?? defaults.BrowserPluginHeartbeatIntervalSeconds,
            AllowExitWithoutPassword = config.AllowExitWithoutPassword ?? defaults.AllowExitWithoutPassword,
            ExitPasswordHashBase64 = config.ExitPasswordHashBase64,
            ExitPasswordSaltBase64 = config.ExitPasswordSaltBase64,
            ExitPasswordIterations = config.ExitPasswordIterations ?? defaults.ExitPasswordIterations
        };
    }

    public AppSettings WithAccessPassword(string password)
    {
        var hash = PasswordHash.Create(password, ExitPasswordIterations);
        return new AppSettings
        {
            ProductName = ProductName,
            CompanyName = CompanyName,
            InstallDirectory = InstallDirectory,
            DataDirectory = DataDirectory,
            ScreenshotDirectory = ScreenshotDirectory,
            ActiveWindowPollInterval = ActiveWindowPollInterval,
            ScreenshotInterval = ScreenshotInterval,
            IdleThreshold = IdleThreshold,
            BrowserReceiverPort = BrowserReceiverPort,
            RetentionDays = RetentionDays,
            ScreenshotRetentionDays = ScreenshotRetentionDays,
            QuietMode = QuietMode,
            ShowNotifications = ShowNotifications,
            BrowserPluginHeartbeatIntervalSeconds = BrowserPluginHeartbeatIntervalSeconds,
            AllowExitWithoutPassword = false,
            ExitPasswordHashBase64 = hash.HashBase64,
            ExitPasswordSaltBase64 = hash.SaltBase64,
            ExitPasswordIterations = hash.Iterations
        };
    }

    public void Save()
    {
        var config = new InstallConfig
        {
            ProductName = ProductName,
            CompanyName = CompanyName,
            DataDirectory = "%APPDATA%\\1984",
            ScreenshotDirectory = ScreenshotDirectory,
            ActiveWindowPollIntervalSeconds = ActiveWindowPollInterval.TotalSeconds,
            ScreenshotIntervalSeconds = ScreenshotInterval.TotalSeconds,
            IdleThresholdSeconds = IdleThreshold.TotalSeconds,
            BrowserReceiverPort = BrowserReceiverPort,
            RetentionDays = RetentionDays,
            ScreenshotRetentionDays = ScreenshotRetentionDays,
            QuietMode = QuietMode,
            ShowNotifications = ShowNotifications,
            BrowserPluginHeartbeatIntervalSeconds = BrowserPluginHeartbeatIntervalSeconds,
            AllowExitWithoutPassword = AllowExitWithoutPassword,
            ExitPasswordHashBase64 = ExitPasswordHashBase64,
            ExitPasswordSaltBase64 = ExitPasswordSaltBase64,
            ExitPasswordIterations = ExitPasswordIterations
        };

        var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(ConfigPath, json);
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
        public string? ScreenshotDirectory { get; set; }
        public double? ActiveWindowPollIntervalSeconds { get; set; }
        public double? ScreenshotIntervalSeconds { get; set; }
        public double? IdleThresholdSeconds { get; set; }
        public int? BrowserReceiverPort { get; set; }
        public int? RetentionDays { get; set; }
        public int? ScreenshotRetentionDays { get; set; }
        public bool? QuietMode { get; set; }
        public bool? ShowNotifications { get; set; }
        public int? BrowserPluginHeartbeatIntervalSeconds { get; set; }
        public bool? AllowExitWithoutPassword { get; set; }
        public string? ExitPasswordHashBase64 { get; set; }
        public string? ExitPasswordSaltBase64 { get; set; }
        public int? ExitPasswordIterations { get; set; }
    }
}
