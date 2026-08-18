using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RecordTime.Core.Models;
using RecordTime.Core.Services;
using RecordTime.Data;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 预算进度项 - 包含预算信息和今日进度
/// </summary>
public class BudgetProgressItem
{
    /// <summary>
    /// 时间预算
    /// </summary>
    public TimeBudget Budget { get; set; } = null!;

    /// <summary>
    /// 今日进度
    /// </summary>
    public DailyBudgetProgress? TodayProgress { get; set; }

    /// <summary>
    /// 今日实际使用分钟数
    /// </summary>
    public int TodayActualMinutes => TodayProgress?.ActualMinutes ?? 0;

    /// <summary>
    /// 完成百分比
    /// </summary>
    public double ProgressPercentage => Budget.TargetMinutes > 0
        ? Math.Min(100.0, (double)TodayActualMinutes / Budget.TargetMinutes * 100)
        : 0;

    /// <summary>
    /// 剩余分钟数
    /// </summary>
    public int RemainingMinutes => Math.Max(0, Budget.TargetMinutes - TodayActualMinutes);

    /// <summary>
    /// 是否超出预算（仅对 Maximum 类型有意义）
    /// </summary>
    public bool IsOverBudget => Budget.Type == BudgetType.Maximum && TodayActualMinutes > Budget.TargetMinutes;

    /// <summary>
    /// 是否达到目标（对 Minimum 类型是达到下限，对 Maximum 类型是未超标）
    /// </summary>
    public bool IsGoalMet => Budget.Type == BudgetType.Maximum
        ? TodayActualMinutes <= Budget.TargetMinutes
        : TodayActualMinutes >= Budget.TargetMinutes;

    /// <summary>
    /// 是否需要发送提醒
    /// </summary>
    public bool NeedsReminder => Budget.ReminderEnabled &&
                                 TodayProgress != null &&
                                 !TodayProgress.ReminderSent &&
                                 ProgressPercentage >= Budget.ReminderThreshold;

    /// <summary>
    /// 状态文本
    /// </summary>
    public string StatusText
    {
        get
        {
            if (Budget.Type == BudgetType.Maximum)
            {
                if (IsOverBudget)
                    return $"已超出 {TodayActualMinutes - Budget.TargetMinutes} 分钟";
                else
                    return $"剩余 {RemainingMinutes} 分钟";
            }
            else
            {
                if (IsGoalMet)
                    return "已达成目标";
                else
                    return $"还需 {RemainingMinutes} 分钟";
            }
        }
    }
}

/// <summary>
/// 时间预算追踪服务 - 追踪和更新预算进度
/// </summary>
public class BudgetTrackingService : IDisposable
{
    private System.Threading.Timer? _updateTimer;
    private readonly SemaphoreSlim _updateLock = new(1, 1);

    /// <summary>
    /// 进度更新事件
    /// </summary>
    public event EventHandler<List<BudgetProgressItem>>? ProgressUpdated;

    /// <summary>
    /// 提醒触发事件
    /// </summary>
    public event EventHandler<BudgetProgressItem>? ReminderTriggered;

    /// <summary>
    /// 追踪服务是否正在运行
    /// </summary>
    public bool IsRunning => _updateTimer != null;

    /// <summary>
    /// 启动追踪（别名）
    /// </summary>
    public void Start() => StartTracking();

    /// <summary>
    /// 停止追踪（别名）
    /// </summary>
    public void Stop() => StopTracking();

    /// <summary>
    /// 立即触发进度更新（用于会话结束时实时同步）
    /// </summary>
    public void TriggerImmediateUpdate()
    {
        if (!IsRunning)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                await UpdateProgressAsync();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "立即更新预算进度时发生错误");
            }
        });
    }

    /// <summary>
    /// 启动定时更新（每分钟更新一次）
    /// </summary>
    public void StartTracking(int intervalSeconds = 60)
    {
        StopTracking();

        _updateTimer = new System.Threading.Timer(
            callback: _ => Task.Run(async () =>
            {
                try
                {
                    await UpdateProgressAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "更新预算进度时发生错误");
                }
            }),
            state: null,
            dueTime: TimeSpan.Zero, // 立即执行一次
            period: TimeSpan.FromSeconds(intervalSeconds)
        );

        Log.Information("预算追踪服务已启动，更新间隔 {Interval} 秒", intervalSeconds);
    }

    /// <summary>
    /// 停止定时更新
    /// </summary>
    public void StopTracking()
    {
        _updateTimer?.Dispose();
        _updateTimer = null;
        Log.Information("预算追踪服务已停止");
    }

    /// <summary>
    /// 获取所有预算的当前进度
    /// </summary>
    public async Task<List<BudgetProgressItem>> GetCurrentProgressAsync()
    {
        await using var context = new RecordTimeDbContext();

        var budgets = await context.TimeBudgets
            .AsNoTracking()
            .Where(b => b.IsEnabled)
            .ToListAsync();

        if (budgets.Count == 0)
            return new List<BudgetProgressItem>();

        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        // 批量获取所有今日进度
        var allProgress = await context.DailyBudgetProgresses
            .AsNoTracking()
            .Where(p => p.Date >= today && p.Date < tomorrow)
            .ToDictionaryAsync(p => p.TimeBudgetId);

        // 批量获取今日使用时间（按 ProcessName 和 Category 聚合）
        var (usageByProcess, usageByCategory) = await GetTodayUsageAsync(context, today, tomorrow);

        var progressItems = new List<BudgetProgressItem>();

        foreach (var budget in budgets)
        {
            int todayMinutes = 0;
            if (!string.IsNullOrEmpty(budget.ProcessName) && usageByProcess.TryGetValue(budget.ProcessName, out var procSecs))
                todayMinutes = procSecs / 60;
            else if (!string.IsNullOrEmpty(budget.Category) && usageByCategory.TryGetValue(budget.Category, out var catSecs))
                todayMinutes = catSecs / 60;

            allProgress.TryGetValue(budget.Id, out var todayProgress);

            if (todayProgress == null || todayProgress.ActualMinutes != todayMinutes)
            {
                todayProgress = new DailyBudgetProgress
                {
                    TimeBudgetId = budget.Id,
                    Date = today,
                    TargetMinutes = budget.TargetMinutes,
                    ActualMinutes = todayMinutes,
                    LastUpdated = DateTime.Now
                };
            }

            progressItems.Add(new BudgetProgressItem { Budget = budget, TodayProgress = todayProgress });
        }

        return progressItems;
    }

    /// <summary>
    /// 计算今日使用时间
    /// </summary>
    private async Task<int> CalculateTodayUsageAsync(RecordTimeDbContext context, TimeBudget budget)
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);

        IQueryable<AppSession> query = context.Sessions
            .AsNoTracking()
            .Where(s => s.StartTime < tomorrow && (s.EndTime == null || s.EndTime >= today));

        if (!string.IsNullOrEmpty(budget.ProcessName))
        {
            // 按应用进程名筛选
            query = query.Where(s => s.ProcessName == budget.ProcessName);
        }
        else if (!string.IsNullOrEmpty(budget.Category))
        {
            // 按分类筛选
            query = query.Where(s => s.Category == budget.Category);
        }
        else
        {
            return 0;
        }

        var sessions = await query.ToListAsync();
        var now = DateTime.Now;
        var totalSeconds = sessions.Sum(s =>
            SessionDurationCalculator.GetDurationSeconds(s.StartTime, s.EndTime, today, tomorrow, now));
        return totalSeconds / 60;
    }

    /// <summary>
    /// 批量计算今日按进程和分类聚合的有效使用秒数。
    /// </summary>
    private static async Task<(Dictionary<string, int> ByProcess, Dictionary<string, int> ByCategory)>
        GetTodayUsageAsync(RecordTimeDbContext context, DateTime today, DateTime tomorrow)
    {
        var now = DateTime.Now;
        var sessions = await context.Sessions.AsNoTracking()
            .Where(s => s.StartTime < tomorrow && (s.EndTime == null || s.EndTime >= today))
            .ToListAsync();

        int GetEffectiveSeconds(AppSession session) =>
            SessionDurationCalculator.GetDurationSeconds(session.StartTime, session.EndTime, today, tomorrow, now);

        var usageByProcess = sessions
            .GroupBy(s => s.ProcessName)
            .ToDictionary(g => g.Key, g => g.Sum(GetEffectiveSeconds));

        var usageByCategory = sessions
            .Where(s => s.Category != null)
            .GroupBy(s => s.Category!)
            .ToDictionary(g => g.Key, g => g.Sum(GetEffectiveSeconds));

        return (usageByProcess, usageByCategory);
    }

    /// <summary>
    /// 更新所有预算进度
    /// </summary>
    public async Task UpdateProgressAsync()
    {
        if (!await _updateLock.WaitAsync(0))
        {
            Log.Debug("预算进度更新正在进行中，跳过本次更新");
            return;
        }

        try
        {
            await using var context = new RecordTimeDbContext();
            var today = DateTime.Today;

            // 获取所有启用的预算
            var budgets = await context.TimeBudgets
                .Where(b => b.IsEnabled)
                .ToListAsync();

            if (budgets.Count == 0)
                return;

            var tomorrow = today.AddDays(1);

            // 批量查询今日使用数据
            var (usageByProcess, usageByCategory) = await GetTodayUsageAsync(context, today, tomorrow);

            // 批量获取今日进度
            var allProgress = await context.DailyBudgetProgresses
                .Where(p => p.Date >= today && p.Date < tomorrow)
                .ToDictionaryAsync(p => p.TimeBudgetId);

            var progressItems = new List<BudgetProgressItem>();

            foreach (var budget in budgets)
            {
                int todayMinutes = 0;
                if (!string.IsNullOrEmpty(budget.ProcessName) && usageByProcess.TryGetValue(budget.ProcessName, out var procSecs))
                    todayMinutes = procSecs / 60;
                else if (!string.IsNullOrEmpty(budget.Category) && usageByCategory.TryGetValue(budget.Category, out var catSecs))
                    todayMinutes = catSecs / 60;

                allProgress.TryGetValue(budget.Id, out var progress);

                if (progress == null)
                {
                    progress = new DailyBudgetProgress
                    {
                        TimeBudgetId = budget.Id,
                        Date = today,
                        TargetMinutes = budget.TargetMinutes,
                        ActualMinutes = todayMinutes,
                        LastUpdated = DateTime.Now
                    };
                    context.DailyBudgetProgresses.Add(progress);
                }
                else
                {
                    progress.ActualMinutes = todayMinutes;
                    progress.LastUpdated = DateTime.Now;
                    progress.GoalReached = budget.Type == BudgetType.Maximum
                        ? todayMinutes <= progress.TargetMinutes
                        : todayMinutes >= progress.TargetMinutes;
                }

                var item = new BudgetProgressItem { Budget = budget, TodayProgress = progress };
                progressItems.Add(item);

                if (item.NeedsReminder)
                {
                    progress.ReminderSent = true;
                    ReminderTriggered?.Invoke(this, item);
                    Log.Information("预算提醒触发: {DisplayName}, 进度 {Progress:F0}%",
                        budget.DisplayName, item.ProgressPercentage);
                }
            }

            await context.SaveChangesAsync();

            // 触发进度更新事件
            ProgressUpdated?.Invoke(this, progressItems);

            Log.Debug("预算进度已更新: {Count} 个预算", progressItems.Count);
        }
        finally
        {
            _updateLock.Release();
        }
    }

    /// <summary>
    /// 获取预算的历史进度
    /// </summary>
    public async Task<List<DailyBudgetProgress>> GetProgressHistoryAsync(int budgetId, int days = 7)
    {
        await using var context = new RecordTimeDbContext();
        var startDate = DateTime.Today.AddDays(-days);

        return await context.DailyBudgetProgresses
            .AsNoTracking()
            .Where(p => p.TimeBudgetId == budgetId && p.Date >= startDate)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    public void Dispose()
    {
        StopTracking();
        _updateLock.Dispose();
    }
}
