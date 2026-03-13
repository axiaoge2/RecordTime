namespace RecordTime.Core.Models;

/// <summary>
/// 应用程序设置配置
/// </summary>
public class AppSettings
{
    /// <summary>
    /// 常规设置
    /// </summary>
    public GeneralSettings General { get; set; } = new();

    /// <summary>
    /// 监控行为设置
    /// </summary>
    public MonitoringSettings Monitoring { get; set; } = new();

    /// <summary>
    /// 隐私设置
    /// </summary>
    public PrivacySettings Privacy { get; set; } = new();

    /// <summary>
    /// 数据管理设置
    /// </summary>
    public DataSettings Data { get; set; } = new();

    /// <summary>
    /// 界面设置
    /// </summary>
    public UISettings UI { get; set; } = new();

    /// <summary>
    /// AI Coach 设置
    /// </summary>
    public AICoachAppSettings? AICoach { get; set; }
}

/// <summary>
/// 常规设置
/// </summary>
public class GeneralSettings
{
    /// <summary>
    /// 开机自动启动
    /// </summary>
    public bool AutoStart { get; set; } = false;

    /// <summary>
    /// 最小化到系统托盘
    /// </summary>
    public bool MinimizeToTray { get; set; } = true;

    /// <summary>
    /// 显示通知
    /// </summary>
    public bool ShowNotifications { get; set; } = true;

    /// <summary>
    /// 语言 (zh-CN, en-US)
    /// </summary>
    public string Language { get; set; } = "zh-CN";
}

/// <summary>
/// 监控行为设置
/// </summary>
public class MonitoringSettings
{
    /// <summary>
    /// 空闲超时时间(分钟)
    /// </summary>
    public int IdleTimeoutMinutes { get; set; } = 5;

    /// <summary>
    /// 窗口轮询间隔(毫秒) - 活跃时
    /// </summary>
    public int ActivePollingIntervalMs { get; set; } = 500;

    /// <summary>
    /// 窗口轮询间隔(毫秒) - 空闲时
    /// </summary>
    public int IdlePollingIntervalMs { get; set; } = 2000;

    /// <summary>
    /// 心跳更新间隔(秒)
    /// </summary>
    public int HeartbeatIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// 键盘活动阈值(30秒内按键次数)
    /// </summary>
    public int KeyboardActivityThreshold { get; set; } = 20;

    /// <summary>
    /// 鼠标点击阈值(30秒内点击次数)
    /// </summary>
    public int MouseClickThreshold { get; set; } = 10;
}

/// <summary>
/// 隐私设置
/// </summary>
public class PrivacySettings
{
    /// <summary>
    /// 记录窗口标题(如果禁用,将不记录任何标题)
    /// </summary>
    public bool RecordWindowTitles { get; set; } = true;

    /// <summary>
    /// 隐私模式启用
    /// </summary>
    public bool PrivacyModeEnabled { get; set; } = false;

    /// <summary>
    /// 排除监控的应用列表(进程名)
    /// </summary>
    public List<string> ExcludedApps { get; set; } = new();

    /// <summary>
    /// 敏感应用列表(不记录窗口标题)
    /// </summary>
    public List<string> SensitiveApps { get; set; } = new()
    {
        // 默认敏感应用
        "keepass", "1password", "lastpass", "bitwarden",
        "chrome", "firefox", "edge", "brave" // 浏览器(密码管理)
    };
}

/// <summary>
/// 数据管理设置
/// </summary>
public class DataSettings
{
    /// <summary>
    /// 自动清理旧数据(天数, 0=禁用)
    /// </summary>
    public int AutoCleanupDays { get; set; } = 0;

    /// <summary>
    /// 清理前自动备份
    /// </summary>
    public bool BackupBeforeCleanup { get; set; } = true;

    /// <summary>
    /// 备份保留天数
    /// </summary>
    public int BackupRetentionDays { get; set; } = 30;
}

/// <summary>
/// 界面设置
/// </summary>
public class UISettings
{
    /// <summary>
    /// 主题 (Light, Dark, Auto)
    /// </summary>
    public string Theme { get; set; } = "Auto";

    /// <summary>
    /// 强调色
    /// </summary>
    public string AccentColor { get; set; } = "#0078D4";
}

/// <summary>
/// AI Coach 设置
/// </summary>
public class AICoachAppSettings
{
    /// <summary>
    /// API 基础 URL (支持 OpenAI 兼容的 API)
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1/";

    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// 温度参数 (0.0-1.0)
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 超时时间 (秒)
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
