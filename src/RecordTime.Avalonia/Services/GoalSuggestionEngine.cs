using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RecordTime.Core.Models;
using RecordTime.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 用户使用模式数据 - 用于 AI 分析
/// </summary>
public class UsagePatternData
{
    /// <summary>
    /// 应用进程名
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 应用分类
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 分析天数
    /// </summary>
    public int DaysAnalyzed { get; set; }

    /// <summary>
    /// 每日平均使用分钟数
    /// </summary>
    public double DailyAverageMinutes { get; set; }

    /// <summary>
    /// 每日最大使用分钟数
    /// </summary>
    public int DailyMaxMinutes { get; set; }

    /// <summary>
    /// 每日最小使用分钟数
    /// </summary>
    public int DailyMinMinutes { get; set; }

    /// <summary>
    /// 总使用分钟数
    /// </summary>
    public int TotalMinutes { get; set; }

    /// <summary>
    /// 使用天数
    /// </summary>
    public int UsageDays { get; set; }

    /// <summary>
    /// 主要活动类型
    /// </summary>
    public ActivityType PrimaryActivityType { get; set; }

    /// <summary>
    /// 使用频率（0-1）
    /// </summary>
    public double UsageFrequency => DaysAnalyzed > 0 ? (double)UsageDays / DaysAnalyzed : 0;
}

/// <summary>
/// 分类使用模式数据
/// </summary>
public class CategoryPatternData
{
    /// <summary>
    /// 分类名称
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// 分析天数
    /// </summary>
    public int DaysAnalyzed { get; set; }

    /// <summary>
    /// 每日平均使用分钟数
    /// </summary>
    public double DailyAverageMinutes { get; set; }

    /// <summary>
    /// 每日最大使用分钟数
    /// </summary>
    public int DailyMaxMinutes { get; set; }

    /// <summary>
    /// 总使用分钟数
    /// </summary>
    public int TotalMinutes { get; set; }

    /// <summary>
    /// 包含的应用数量
    /// </summary>
    public int AppCount { get; set; }

    /// <summary>
    /// 主要活动类型
    /// </summary>
    public ActivityType PrimaryActivityType { get; set; }
}

/// <summary>
/// 目标建议引擎 - 基于用户历史数据生成智能建议
/// </summary>
public class GoalSuggestionEngine
{
    // 分类权重配置 - 某些分类更值得设置目标
    private static readonly Dictionary<string, int> CategoryPriority = new()
    {
        { "视频娱乐", 5 },
        { "游戏", 5 },
        { "社交通讯", 4 },
        { "新闻资讯", 3 },
        { "开发工具", 2 },
        { "办公软件", 2 },
        { "系统工具", 1 },
        { "其他", 1 }
    };

    /// <summary>
    /// 分析用户使用模式
    /// </summary>
    /// <param name="days">分析的天数</param>
    /// <returns>应用使用模式列表</returns>
    public async Task<List<UsagePatternData>> AnalyzeAppPatternsAsync(int days = 14)
    {
        await using var context = new RecordTimeDbContext();

        var startDate = DateTime.Today.AddDays(-days);
        var endDate = DateTime.Today;

        var sessions = await context.Sessions
            .AsNoTracking()
            .Where(s => s.StartTime >= startDate && s.StartTime < endDate.AddDays(1) && s.EndTime != null)
            .ToListAsync();

        if (sessions.Count == 0)
            return new List<UsagePatternData>();

        // 按应用分组统计
        var appGroups = sessions
            .GroupBy(s => s.ProcessName)
            .Select(g =>
            {
                var dailyUsage = g
                    .GroupBy(s => s.StartTime.Date)
                    .Select(dg => dg.Sum(s => s.DurationSeconds) / 60)
                    .ToList();

                var firstSession = g.First();
                var activityTypes = g.GroupBy(s => s.ActivityType)
                    .OrderByDescending(ag => ag.Count())
                    .First().Key;

                return new UsagePatternData
                {
                    ProcessName = g.Key,
                    DisplayName = firstSession.DisplayName,
                    Category = firstSession.Category ?? "其他",
                    DaysAnalyzed = days,
                    DailyAverageMinutes = dailyUsage.Average(),
                    DailyMaxMinutes = dailyUsage.Max(),
                    DailyMinMinutes = dailyUsage.Min(),
                    TotalMinutes = dailyUsage.Sum(),
                    UsageDays = dailyUsage.Count,
                    PrimaryActivityType = activityTypes
                };
            })
            .Where(p => p.TotalMinutes >= 30) // 至少使用 30 分钟的应用才纳入分析
            .OrderByDescending(p => p.TotalMinutes)
            .ToList();

        return appGroups;
    }

    /// <summary>
    /// 分析分类使用模式
    /// </summary>
    public async Task<List<CategoryPatternData>> AnalyzeCategoryPatternsAsync(int days = 14)
    {
        await using var context = new RecordTimeDbContext();

        var startDate = DateTime.Today.AddDays(-days);
        var endDate = DateTime.Today;

        var sessions = await context.Sessions
            .AsNoTracking()
            .Where(s => s.StartTime >= startDate && s.StartTime < endDate.AddDays(1) && s.EndTime != null)
            .ToListAsync();

        if (sessions.Count == 0)
            return new List<CategoryPatternData>();

        // 按分类分组统计
        var categoryGroups = sessions
            .GroupBy(s => s.Category)
            .Select(g =>
            {
                var dailyUsage = g
                    .GroupBy(s => s.StartTime.Date)
                    .Select(dg => dg.Sum(s => s.DurationSeconds) / 60)
                    .ToList();

                var activityTypes = g.GroupBy(s => s.ActivityType)
                    .OrderByDescending(ag => ag.Count())
                    .First().Key;

                return new CategoryPatternData
                {
                    Category = g.Key ?? "其他",
                    DaysAnalyzed = days,
                    DailyAverageMinutes = dailyUsage.Average(),
                    DailyMaxMinutes = dailyUsage.Max(),
                    TotalMinutes = dailyUsage.Sum(),
                    AppCount = g.Select(s => s.ProcessName).Distinct().Count(),
                    PrimaryActivityType = activityTypes
                };
            })
            .Where(p => p.TotalMinutes >= 60) // 至少使用 1 小时的分类才纳入分析
            .OrderByDescending(p => p.TotalMinutes)
            .ToList();

        return categoryGroups;
    }

    /// <summary>
    /// 生成目标建议（基于规则引擎）
    /// </summary>
    public async Task<List<GoalSuggestion>> GenerateSuggestionsAsync(int days = 14)
    {
        await using var context = new RecordTimeDbContext();

        var suggestions = new List<GoalSuggestion>();

        // 获取现有预算，避免重复建议
        var existingBudgets = await context.TimeBudgets.AsNoTracking().ToListAsync();
        var existingProcessNames = existingBudgets.Where(b => b.ProcessName != null).Select(b => b.ProcessName).ToHashSet();
        var existingCategories = existingBudgets.Where(b => b.Category != null).Select(b => b.Category).ToHashSet();

        // 分析应用使用模式
        var appPatterns = await AnalyzeAppPatternsAsync(days);

        // 为高使用量的娱乐类应用生成上限建议
        foreach (var pattern in appPatterns)
        {
            if (existingProcessNames.Contains(pattern.ProcessName))
                continue;

            var suggestion = GenerateAppSuggestion(pattern, days);
            if (suggestion != null)
            {
                suggestions.Add(suggestion);
            }
        }

        // 分析分类使用模式
        var categoryPatterns = await AnalyzeCategoryPatternsAsync(days);

        // 为高使用量分类生成建议
        foreach (var pattern in categoryPatterns)
        {
            if (existingCategories.Contains(pattern.Category))
                continue;

            var suggestion = GenerateCategorySuggestion(pattern, days);
            if (suggestion != null)
            {
                suggestions.Add(suggestion);
            }
        }

        // 按优先级排序，只返回前 5 个建议
        return suggestions
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.HistoricalAverageMinutes)
            .Take(5)
            .ToList();
    }

    /// <summary>
    /// 为应用生成建议
    /// </summary>
    private GoalSuggestion? GenerateAppSuggestion(UsagePatternData pattern, int days)
    {
        // 基于活动类型和使用量判断是否需要建议
        var isEntertainment = pattern.PrimaryActivityType == ActivityType.Video ||
                             pattern.PrimaryActivityType == ActivityType.Gaming ||
                             pattern.Category == "视频娱乐" ||
                             pattern.Category == "游戏";

        var isProductive = pattern.Category == "开发工具" ||
                          pattern.Category == "办公软件" ||
                          pattern.PrimaryActivityType == ActivityType.ActiveTyping;

        // 计算建议的目标
        int suggestedMinutes;
        BudgetType suggestedType;
        string reason;
        int priority;

        if (isEntertainment)
        {
            // 娱乐类：建议设置上限，目标为当前使用量的 80%
            suggestedType = BudgetType.Maximum;
            suggestedMinutes = (int)(pattern.DailyAverageMinutes * 0.8);
            suggestedMinutes = Math.Max(30, RoundToNearest15(suggestedMinutes)); // 至少 30 分钟，四舍五入到 15 分钟
            reason = $"过去 {days} 天平均每天使用 {pattern.DailyAverageMinutes:F0} 分钟，建议适当控制娱乐时间";
            priority = CategoryPriority.GetValueOrDefault(pattern.Category, 3);
        }
        else if (isProductive && pattern.UsageFrequency > 0.7)
        {
            // 高频使用的生产力工具：建议设置下限，鼓励保持
            suggestedType = BudgetType.Minimum;
            suggestedMinutes = (int)(pattern.DailyAverageMinutes * 0.9);
            suggestedMinutes = Math.Max(30, RoundToNearest15(suggestedMinutes));
            reason = $"过去 {days} 天使用频率 {pattern.UsageFrequency:P0}，建议保持良好的工作习惯";
            priority = 2;
        }
        else if (pattern.DailyAverageMinutes > 120)
        {
            // 其他高使用量应用：建议设置上限
            suggestedType = BudgetType.Maximum;
            suggestedMinutes = (int)(pattern.DailyAverageMinutes * 0.85);
            suggestedMinutes = Math.Max(60, RoundToNearest15(suggestedMinutes));
            reason = $"过去 {days} 天平均每天使用 {pattern.DailyAverageMinutes:F0} 分钟，使用时间较长";
            priority = 3;
        }
        else
        {
            // 使用量不高，暂不建议
            return null;
        }

        return new GoalSuggestion
        {
            ProcessName = pattern.ProcessName,
            DisplayName = pattern.DisplayName,
            SuggestedType = suggestedType,
            SuggestedMinutes = suggestedMinutes,
            Reason = reason,
            Priority = priority,
            BasedOnDays = days,
            HistoricalAverageMinutes = (int)pattern.DailyAverageMinutes,
            ExpiresAt = DateTime.Now.AddDays(7)
        };
    }

    /// <summary>
    /// 为分类生成建议
    /// </summary>
    private GoalSuggestion? GenerateCategorySuggestion(CategoryPatternData pattern, int days)
    {
        var priority = CategoryPriority.GetValueOrDefault(pattern.Category, 2);

        // 只为高优先级分类生成建议
        if (priority < 3)
            return null;

        // 只有当分类使用量较高时才建议
        if (pattern.DailyAverageMinutes < 60)
            return null;

        var isEntertainment = pattern.Category == "视频娱乐" ||
                             pattern.Category == "游戏" ||
                             pattern.Category == "社交通讯";

        int suggestedMinutes;
        BudgetType suggestedType;
        string reason;

        if (isEntertainment)
        {
            suggestedType = BudgetType.Maximum;
            suggestedMinutes = (int)(pattern.DailyAverageMinutes * 0.75);
            suggestedMinutes = Math.Max(60, RoundToNearest15(suggestedMinutes));
            reason = $"过去 {days} 天「{pattern.Category}」平均每天 {pattern.DailyAverageMinutes:F0} 分钟，建议设置每日上限";
        }
        else
        {
            return null;
        }

        return new GoalSuggestion
        {
            Category = pattern.Category,
            DisplayName = pattern.Category,
            SuggestedType = suggestedType,
            SuggestedMinutes = suggestedMinutes,
            Reason = reason,
            Priority = priority,
            BasedOnDays = days,
            HistoricalAverageMinutes = (int)pattern.DailyAverageMinutes,
            ExpiresAt = DateTime.Now.AddDays(7)
        };
    }

    /// <summary>
    /// 四舍五入到最近的 15 分钟
    /// </summary>
    private static int RoundToNearest15(int minutes)
    {
        return ((minutes + 7) / 15) * 15;
    }
}
