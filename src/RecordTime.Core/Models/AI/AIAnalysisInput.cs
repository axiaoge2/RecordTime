namespace RecordTime.Core.Models.AI;

/// <summary>
/// AI 分析输入数据（脱敏后）
/// </summary>
public class AIAnalysisInput
{
    /// <summary>
    /// 分析开始日期
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// 分析结束日期
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// 总活跃时长（小时）
    /// </summary>
    public double TotalActiveHours { get; set; }

    /// <summary>
    /// 按分类统计的使用时长（小时）
    /// 例如: { "开发工具": 18.2, "浏览器": 12.5 }
    /// </summary>
    public Dictionary<string, double> CategoryHours { get; set; } = new();

    /// <summary>
    /// 按活动类型统计的使用时长（小时）
    /// 例如: { "ActiveTyping": 22.0, "Video": 6.0 }
    /// </summary>
    public Dictionary<string, double> ActivityHours { get; set; } = new();

    /// <summary>
    /// 会话总数
    /// </summary>
    public int SessionCount { get; set; }

    /// <summary>
    /// 使用的应用数量
    /// </summary>
    public int UniqueAppCount { get; set; }

    /// <summary>
    /// 平均会话时长（分钟）
    /// </summary>
    public double AvgSessionMinutes { get; set; }

    /// <summary>
    /// 最活跃的小时 (0-23)
    /// </summary>
    public int PeakHour { get; set; }

    /// <summary>
    /// 每日使用时长趋势（小时）
    /// </summary>
    public Dictionary<string, double> DailyHours { get; set; } = new();

    /// <summary>
    /// 每小时使用时长分布（小时）
    /// </summary>
    public Dictionary<int, double> HourlyDistribution { get; set; } = new();

    /// <summary>
    /// 隐私级别
    /// </summary>
    public AIPrivacyLevel PrivacyLevel { get; set; } = AIPrivacyLevel.CategoryOnly;

    /// <summary>
    /// Top 应用列表（仅当 PrivacyLevel 允许时包含）
    /// </summary>
    public List<TopAppInfo>? TopApps { get; set; }
}

/// <summary>
/// 隐私级别
/// </summary>
public enum AIPrivacyLevel
{
    /// <summary>
    /// 仅发送分类数据（最安全）
    /// </summary>
    CategoryOnly,

    /// <summary>
    /// 包含通用化的应用名称
    /// </summary>
    IncludeGeneralizedApps
}

/// <summary>
/// Top 应用信息（通用化名称）
/// </summary>
public class TopAppInfo
{
    /// <summary>
    /// 通用化的应用名称（如 "Code Editor" 而非 "Visual Studio Code"）
    /// </summary>
    public string GeneralizedName { get; set; } = string.Empty;

    /// <summary>
    /// 分类
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 使用时长（小时）
    /// </summary>
    public double Hours { get; set; }

    /// <summary>
    /// 占比（百分比）
    /// </summary>
    public double Percentage { get; set; }
}
