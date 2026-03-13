using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RecordTime.Core.Models;
using RecordTime.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 日末总结数据
/// </summary>
public class DailySummaryData
{
    /// <summary>
    /// 总结日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 总使用时长（分钟）
    /// </summary>
    public int TotalMinutes { get; set; }

    /// <summary>
    /// 会话总数
    /// </summary>
    public int SessionCount { get; set; }

    /// <summary>
    /// 应用数量
    /// </summary>
    public int AppCount { get; set; }

    /// <summary>
    /// TOP 3 应用
    /// </summary>
    public List<AppUsageSummary> TopApps { get; set; } = new();

    /// <summary>
    /// 预算完成情况
    /// </summary>
    public List<BudgetSummary> BudgetResults { get; set; } = new();

    /// <summary>
    /// 格式化的总时长
    /// </summary>
    public string TotalDurationText
    {
        get
        {
            var hours = TotalMinutes / 60;
            var mins = TotalMinutes % 60;
            if (hours > 0 && mins > 0)
                return $"{hours}小时{mins}分钟";
            else if (hours > 0)
                return $"{hours}小时";
            else
                return $"{mins}分钟";
        }
    }
}

/// <summary>
/// 应用使用总结
/// </summary>
public class AppUsageSummary
{
    public string AppName { get; set; } = string.Empty;
    public int Minutes { get; set; }
    public double Percentage { get; set; }

    public string DurationText
    {
        get
        {
            var hours = Minutes / 60;
            var mins = Minutes % 60;
            if (hours > 0)
                return $"{hours}h{mins}m";
            else
                return $"{mins}m";
        }
    }
}

/// <summary>
/// 预算完成总结
/// </summary>
public class BudgetSummary
{
    public string BudgetName { get; set; } = string.Empty;
    public BudgetType Type { get; set; }
    public int TargetMinutes { get; set; }
    public int ActualMinutes { get; set; }
    public bool GoalMet { get; set; }

    public string StatusText => GoalMet ? "✓ 达成" : "✗ 未达成";
    public string StatusEmoji => GoalMet ? "🎉" : "📈";
}

/// <summary>
/// 日末总结服务 - 在每天结束时生成使用总结
/// </summary>
public class DailySummaryService : IDisposable
{
    private static DailySummaryService? _instance;
    public static DailySummaryService Instance => _instance ??= new DailySummaryService();

    private System.Threading.Timer? _checkTimer;
    private DateTime _lastSummaryDate = DateTime.MinValue;
    private readonly int _summaryHour = 22; // 默认在 22:00 生成总结

    /// <summary>
    /// 日末总结事件
    /// </summary>
    public event EventHandler<DailySummaryData>? DailySummaryGenerated;

    /// <summary>
    /// 启动日末总结服务
    /// </summary>
    public void Start()
    {
        Stop();

        // 每分钟检查一次是否到了总结时间
        _checkTimer = new System.Threading.Timer(
            callback: _ => Task.Run(async () =>
            {
                try
                {
                    await CheckAndGenerateSummaryAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "检查日末总结时发生错误");
                }
            }),
            state: null,
            dueTime: TimeSpan.FromMinutes(1),
            period: TimeSpan.FromMinutes(1)
        );

        Log.Information("日末总结服务已启动，将在每天 {Hour}:00 生成总结", _summaryHour);
    }

    /// <summary>
    /// 停止日末总结服务
    /// </summary>
    public void Stop()
    {
        _checkTimer?.Dispose();
        _checkTimer = null;
    }

    /// <summary>
    /// 检查是否需要生成总结
    /// </summary>
    private async Task CheckAndGenerateSummaryAsync()
    {
        var now = DateTime.Now;
        var today = DateTime.Today;

        // 检查是否到了总结时间（22:00 ~ 22:59）
        if (now.Hour == _summaryHour && _lastSummaryDate.Date != today)
        {
            Log.Information("到达日末总结时间，正在生成今日总结...");
            _lastSummaryDate = today;

            var summary = await GenerateSummaryAsync(today);
            if (summary != null)
            {
                DailySummaryGenerated?.Invoke(this, summary);
                Log.Information("日末总结已生成: 总时长 {TotalMinutes} 分钟, {SessionCount} 个会话",
                    summary.TotalMinutes, summary.SessionCount);
            }
        }
    }

    /// <summary>
    /// 手动生成指定日期的总结
    /// </summary>
    public async Task<DailySummaryData?> GenerateSummaryAsync(DateTime date)
    {
        try
        {
            await using var context = new RecordTimeDbContext();

            var targetDate = date.Date;
            var nextDay = targetDate.AddDays(1);

            var baseQuery = context.Sessions.AsNoTracking()
                .Where(s => s.StartTime >= targetDate && s.StartTime < nextDay);

            var stats = await baseQuery
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    TotalSeconds = g.Sum(s => s.DurationSeconds),
                    Count = g.Count(),
                    AppCount = g.Select(s => s.ProcessName).Distinct().Count()
                })
                .FirstOrDefaultAsync();

            if (stats == null || stats.Count == 0)
            {
                Log.Debug("日期 {Date} 没有使用记录，跳过总结", targetDate);
                return null;
            }

            var summary = new DailySummaryData
            {
                Date = targetDate,
                TotalMinutes = stats.TotalSeconds / 60,
                SessionCount = stats.Count,
                AppCount = stats.AppCount
            };

            var topApps = await baseQuery
                .GroupBy(s => new { s.ProcessName, s.DisplayName })
                .Select(g => new
                {
                    AppName = g.Key.DisplayName ?? g.Key.ProcessName,
                    Minutes = g.Sum(s => s.DurationSeconds) / 60
                })
                .OrderByDescending(x => x.Minutes)
                .Take(3)
                .ToListAsync();

            var totalMinutes = summary.TotalMinutes > 0 ? summary.TotalMinutes : 1;
            summary.TopApps = topApps.Select(a => new AppUsageSummary
            {
                AppName = a.AppName,
                Minutes = a.Minutes,
                Percentage = (double)a.Minutes / totalMinutes * 100
            }).ToList();

            // 预算完成情况
            var budgetProgresses = await context.DailyBudgetProgresses
                .AsNoTracking()
                .Include(p => p.TimeBudget)
                .Where(p => p.Date.Date == targetDate)
                .ToListAsync();

            summary.BudgetResults = budgetProgresses
                .Where(p => p.TimeBudget != null)
                .Select(p => new BudgetSummary
                {
                    BudgetName = p.TimeBudget!.DisplayName,
                    Type = p.TimeBudget.Type,
                    TargetMinutes = p.TargetMinutes,
                    ActualMinutes = p.ActualMinutes,
                    GoalMet = p.GoalReached
                }).ToList();

            return summary;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "生成日末总结失败");
            return null;
        }
    }

    /// <summary>
    /// 立即生成今日总结（用于测试）
    /// </summary>
    public async Task TriggerSummaryNowAsync()
    {
        var summary = await GenerateSummaryAsync(DateTime.Today);
        if (summary != null)
        {
            DailySummaryGenerated?.Invoke(this, summary);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
