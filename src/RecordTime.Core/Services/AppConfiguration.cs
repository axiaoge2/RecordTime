namespace RecordTime.Core.Services;

/// <summary>
/// 应用配置模型 - 与 appsettings.json 结构对应
/// </summary>
public class AppConfiguration
{
    public AppInfo App { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public MonitoringConfig Monitoring { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public UIConfig UI { get; set; } = new();
    public ReportsConfig Reports { get; set; } = new();
}

public class AppInfo
{
    public string Name { get; set; } = "RecordTime";
    public string Version { get; set; } = "1.0.0";
}

public class DatabaseConfig
{
    public string FileName { get; set; } = "recordtime.db";
    public int BackupRetentionDays { get; set; } = 7;
    public bool AutoBackupEnabled { get; set; } = true;
}

public class MonitoringConfig
{
    public int WindowPollIntervalMs { get; set; } = 2000;
    public int DataRefreshIntervalMs { get; set; } = 1000;
    public int IdleTimeoutSeconds { get; set; } = 300;
}

public class LoggingConfig
{
    public string MinimumLevel { get; set; } = "Debug";
    public int RetentionDays { get; set; } = 7;
    public ConsoleLoggingConfig Console { get; set; } = new();
    public FileLoggingConfig File { get; set; } = new();
}

public class ConsoleLoggingConfig
{
    public bool Enabled { get; set; } = true;
}

public class FileLoggingConfig
{
    public bool Enabled { get; set; } = true;
}

public class UIConfig
{
    public string Theme { get; set; } = "Light";
    public string DefaultDateFormat { get; set; } = "yyyy年MM月dd日";
    public bool EnableAnimations { get; set; } = true;
}

public class ReportsConfig
{
    public string DefaultExportPath { get; set; } = "";
    public string DefaultFormat { get; set; } = "HTML";
}
