namespace RecordTime.Core.Models.AICoach;

/// <summary>
/// 认知上下文 - 用于构建 Prompt 的当前状态数据
/// </summary>
public class CognitiveContext
{
    /// <summary>
    /// 当前时间
    /// </summary>
    public DateTime CurrentTime { get; set; } = DateTime.Now;

    /// <summary>
    /// 今日摘要
    /// </summary>
    public TodaySummary TodaySummary { get; set; } = new();

    /// <summary>
    /// 当前会话
    /// </summary>
    public CurrentSessionInfo? CurrentSession { get; set; }

    /// <summary>
    /// 最近模式
    /// </summary>
    public RecentPattern RecentPattern { get; set; } = new();

    /// <summary>
    /// 个人参数
    /// </summary>
    public UserParameters? PersonalParams { get; set; }

    /// <summary>
    /// 转换为 Prompt 数据结构（JSON 格式）
    /// </summary>
    public object ToPromptData()
    {
        return new
        {
            currentTime = CurrentTime.ToString("yyyy-MM-ddTHH:mm:ss"),
            todaySummary = new
            {
                totalFocusMinutes = TodaySummary.TotalFocusMinutes,
                totalIdleMinutes = TodaySummary.TotalIdleMinutes,
                appSwitchCount = TodaySummary.AppSwitchCount,
                byCategory = TodaySummary.ByCategory,
                byApp = TodaySummary.ByApp
            },
            currentSession = CurrentSession == null ? null : new
            {
                app = CurrentSession.App,
                category = CurrentSession.Category,
                startTime = CurrentSession.StartTime.ToString("yyyy-MM-ddTHH:mm:ss"),
                durationMinutes = CurrentSession.DurationMinutes,
                activityType = CurrentSession.ActivityType.ToString()
            },
            recentPattern = new
            {
                switchesLast5Min = RecentPattern.SwitchesLast5Min,
                isDeepFocus = RecentPattern.IsDeepFocus,
                isFragmented = RecentPattern.IsFragmented
            },
            personalParams = PersonalParams == null ? null : new
            {
                focusCycleLength = PersonalParams.FocusCycleLength.GetEffectiveValue(),
                peakHours = PersonalParams.PeakHours.GetEffectiveValue(),
                confidence = PersonalParams.FocusCycleLength.Confidence,
                learningPhase = PersonalParams.CurrentPhase.ToString()
            }
        };
    }
}

/// <summary>
/// 今日摘要
/// </summary>
public class TodaySummary
{
    /// <summary>
    /// 总专注时间（分钟）
    /// </summary>
    public int TotalFocusMinutes { get; set; }

    /// <summary>
    /// 总空闲时间（分钟）
    /// </summary>
    public int TotalIdleMinutes { get; set; }

    /// <summary>
    /// 应用切换总次数
    /// </summary>
    public int AppSwitchCount { get; set; }

    /// <summary>
    /// 按分类统计（分钟）
    /// </summary>
    public Dictionary<string, int> ByCategory { get; set; } = new();

    /// <summary>
    /// 按应用统计（分钟）
    /// </summary>
    public Dictionary<string, int> ByApp { get; set; } = new();
}

/// <summary>
/// 当前会话信息
/// </summary>
public class CurrentSessionInfo
{
    /// <summary>
    /// 应用名称
    /// </summary>
    public string App { get; set; } = string.Empty;

    /// <summary>
    /// 应用分类
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 持续时间（分钟）
    /// </summary>
    public int DurationMinutes { get; set; }

    /// <summary>
    /// 活动类型
    /// </summary>
    public ActivityType ActivityType { get; set; }
}

/// <summary>
/// 最近模式
/// </summary>
public class RecentPattern
{
    /// <summary>
    /// 最近5分钟切换次数
    /// </summary>
    public int SwitchesLast5Min { get; set; }

    /// <summary>
    /// 是否深度专注
    /// </summary>
    public bool IsDeepFocus { get; set; }

    /// <summary>
    /// 是否注意力碎片化
    /// </summary>
    public bool IsFragmented { get; set; }
}
