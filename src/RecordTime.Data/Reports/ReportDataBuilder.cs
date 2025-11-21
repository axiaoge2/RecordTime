using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;
using RecordTime.Core.Models.AI;

namespace RecordTime.Data.Reports;

/// <summary>
/// 报告数据构建器 - 从数据库构建 AI 分析所需的结构化数据
/// </summary>
public class ReportDataBuilder
{
    private readonly RecordTimeDbContext _dbContext;

    public ReportDataBuilder(RecordTimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 构建 AI 分析输入数据
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <param name="privacyLevel">隐私级别</param>
    /// <returns>AI 分析输入数据</returns>
    public async Task<AIAnalysisInput> BuildAnalysisInputAsync(
        DateTime startDate,
        DateTime endDate,
        AIPrivacyLevel privacyLevel = AIPrivacyLevel.CategoryOnly)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        // 查询时间范围内的所有会话
        var sessions = await _dbContext.Sessions
            .Where(s => s.StartTime >= start && s.StartTime < end && s.EndTime != null)
            .AsNoTracking()
            .ToListAsync();

        if (sessions.Count == 0)
        {
            return new AIAnalysisInput
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalActiveHours = 0,
                SessionCount = 0,
                UniqueAppCount = 0,
                PrivacyLevel = privacyLevel
            };
        }

        // 计算总时长（秒）
        var totalSeconds = sessions.Sum(s => s.DurationSeconds);
        var totalHours = totalSeconds / 3600.0;

        // 按分类统计
        var categoryHours = sessions
            .GroupBy(s => s.Category ?? "未分类")
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.DurationSeconds) / 3600.0
            );

        // 按活动类型统计
        var activityHours = sessions
            .GroupBy(s => s.ActivityType)
            .ToDictionary(
                g => g.Key.ToString(),
                g => g.Sum(s => s.DurationSeconds) / 3600.0
            );

        // 每日使用统计
        var dailyHours = sessions
            .GroupBy(s => s.StartTime.Date)
            .OrderBy(g => g.Key)
            .ToDictionary(
                g => g.Key.ToString("MM-dd"),
                g => g.Sum(s => s.DurationSeconds) / 3600.0
            );

        // 每小时分布
        var hourlyDistribution = sessions
            .GroupBy(s => s.StartTime.Hour)
            .ToDictionary(
                g => g.Key,
                g => g.Sum(s => s.DurationSeconds) / 3600.0
            );

        // 找出最活跃的小时
        var peakHour = hourlyDistribution.Count > 0
            ? hourlyDistribution.OrderByDescending(kv => kv.Value).First().Key
            : 9; // 默认 9 点

        // 计算平均会话时长（分钟）
        var avgSessionMinutes = sessions.Count > 0
            ? sessions.Average(s => s.DurationSeconds) / 60.0
            : 0;

        var input = new AIAnalysisInput
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalActiveHours = totalHours,
            CategoryHours = categoryHours,
            ActivityHours = activityHours,
            SessionCount = sessions.Count,
            UniqueAppCount = sessions.Select(s => s.ProcessName).Distinct().Count(),
            AvgSessionMinutes = avgSessionMinutes,
            PeakHour = peakHour,
            DailyHours = dailyHours,
            HourlyDistribution = hourlyDistribution,
            PrivacyLevel = privacyLevel
        };

        // 根据隐私级别添加 Top 应用信息
        if (privacyLevel == AIPrivacyLevel.IncludeGeneralizedApps)
        {
            input.TopApps = BuildTopApps(sessions, totalSeconds);
        }

        return input;
    }

    /// <summary>
    /// 构建 Top 应用列表（通用化名称）
    /// </summary>
    private List<TopAppInfo> BuildTopApps(List<AppSession> sessions, int totalSeconds)
    {
        return sessions
            .GroupBy(s => s.ProcessName)
            .Select(g => new
            {
                ProcessName = g.Key,
                DisplayName = g.First().DisplayName ?? g.Key,
                Category = g.First().Category ?? "未分类",
                TotalSeconds = g.Sum(s => s.DurationSeconds)
            })
            .OrderByDescending(x => x.TotalSeconds)
            .Take(10)
            .Select(x => new TopAppInfo
            {
                GeneralizedName = GeneralizeAppName(x.ProcessName, x.DisplayName),
                Category = x.Category,
                Hours = x.TotalSeconds / 3600.0,
                Percentage = (double)x.TotalSeconds / totalSeconds * 100
            })
            .ToList();
    }

    /// <summary>
    /// 通用化应用名称（保护隐私）
    /// </summary>
    private string GeneralizeAppName(string processName, string displayName)
    {
        var lower = processName.ToLower();

        // 浏览器
        if (lower.Contains("chrome") || lower.Contains("firefox") ||
            lower.Contains("edge") || lower.Contains("opera") ||
            lower.Contains("brave") || lower.Contains("vivaldi"))
        {
            return "浏览器";
        }

        // 代码编辑器
        if (lower.Contains("code") || lower.Contains("vscode") ||
            lower.Contains("devenv") || lower.Contains("rider") ||
            lower.Contains("idea") || lower.Contains("pycharm") ||
            lower.Contains("webstorm") || lower.Contains("sublime") ||
            lower.Contains("atom") || lower.Contains("notepad++"))
        {
            return "代码编辑器";
        }

        // 终端
        if (lower.Contains("terminal") || lower.Contains("cmd") ||
            lower.Contains("powershell") || lower.Contains("wt") ||
            lower.Contains("conhost"))
        {
            return "终端";
        }

        // 通讯工具
        if (lower.Contains("wechat") || lower.Contains("qq") ||
            lower.Contains("telegram") || lower.Contains("slack") ||
            lower.Contains("teams") || lower.Contains("discord"))
        {
            return "通讯软件";
        }

        // 视频播放器
        if (lower.Contains("potplayer") || lower.Contains("vlc") ||
            lower.Contains("mpc") || lower.Contains("kmplayer"))
        {
            return "视频播放器";
        }

        // 办公软件
        if (lower.Contains("word") || lower.Contains("excel") ||
            lower.Contains("powerpoint") || lower.Contains("onenote") ||
            lower.Contains("wps"))
        {
            return "办公软件";
        }

        // 游戏平台
        if (lower.Contains("steam") || lower.Contains("epic") ||
            lower.Contains("origin") || lower.Contains("uplay"))
        {
            return "游戏平台";
        }

        // 文件管理器
        if (lower.Contains("explorer"))
        {
            return "文件管理器";
        }

        // 默认返回分类或通用名称
        return displayName.Length > 20 ? "其他应用" : displayName;
    }

    /// <summary>
    /// 获取指定时间范围的统计摘要
    /// </summary>
    public async Task<ReportSummary> GetReportSummaryAsync(DateTime startDate, DateTime endDate)
    {
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var sessions = await _dbContext.Sessions
            .Where(s => s.StartTime >= start && s.StartTime < end && s.EndTime != null)
            .AsNoTracking()
            .ToListAsync();

        var totalSeconds = sessions.Sum(s => s.DurationSeconds);

        return new ReportSummary
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = (int)(endDate - startDate).TotalDays + 1,
            TotalSessions = sessions.Count,
            TotalApps = sessions.Select(s => s.ProcessName).Distinct().Count(),
            TotalHours = totalSeconds / 3600.0,
            AvgDailyHours = totalSeconds / 3600.0 / Math.Max(1, (endDate - startDate).TotalDays + 1)
        };
    }
}

/// <summary>
/// 报告摘要
/// </summary>
public class ReportSummary
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public int TotalSessions { get; set; }
    public int TotalApps { get; set; }
    public double TotalHours { get; set; }
    public double AvgDailyHours { get; set; }
}
