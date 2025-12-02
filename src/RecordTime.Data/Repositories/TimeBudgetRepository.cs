using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;
using RecordTime.Data;

namespace RecordTime.Data.Repositories;

/// <summary>
/// 时间预算仓储接口
/// </summary>
public interface ITimeBudgetRepository : IDisposable
{
    /// <summary>
    /// 获取所有启用的时间预算
    /// </summary>
    Task<List<TimeBudget>> GetEnabledBudgetsAsync();

    /// <summary>
    /// 获取所有时间预算
    /// </summary>
    Task<List<TimeBudget>> GetAllBudgetsAsync();

    /// <summary>
    /// 根据ID获取时间预算
    /// </summary>
    Task<TimeBudget?> GetByIdAsync(int id);

    /// <summary>
    /// 根据进程名获取时间预算
    /// </summary>
    Task<TimeBudget?> GetByProcessNameAsync(string processName);

    /// <summary>
    /// 根据分类获取时间预算
    /// </summary>
    Task<TimeBudget?> GetByCategoryAsync(string category);

    /// <summary>
    /// 添加时间预算
    /// </summary>
    Task<int> AddAsync(TimeBudget budget);

    /// <summary>
    /// 更新时间预算
    /// </summary>
    Task UpdateAsync(TimeBudget budget);

    /// <summary>
    /// 删除时间预算
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// 获取今日进度
    /// </summary>
    Task<DailyBudgetProgress?> GetTodayProgressAsync(int budgetId);

    /// <summary>
    /// 获取指定日期的进度
    /// </summary>
    Task<DailyBudgetProgress?> GetProgressAsync(int budgetId, DateTime date);

    /// <summary>
    /// 获取某个预算的历史进度
    /// </summary>
    Task<List<DailyBudgetProgress>> GetProgressHistoryAsync(int budgetId, int days);

    /// <summary>
    /// 更新或创建每日进度
    /// </summary>
    Task<DailyBudgetProgress> UpdateOrCreateProgressAsync(int budgetId, DateTime date, int actualMinutes);

    /// <summary>
    /// 标记提醒已发送
    /// </summary>
    Task MarkReminderSentAsync(int progressId);

    /// <summary>
    /// 获取所有未处理的目标建议
    /// </summary>
    Task<List<GoalSuggestion>> GetUnprocessedSuggestionsAsync();

    /// <summary>
    /// 添加目标建议
    /// </summary>
    Task<int> AddSuggestionAsync(GoalSuggestion suggestion);

    /// <summary>
    /// 处理目标建议
    /// </summary>
    Task ProcessSuggestionAsync(int suggestionId, string result);

    /// <summary>
    /// 清理过期的建议
    /// </summary>
    Task CleanupExpiredSuggestionsAsync();
}

/// <summary>
/// 时间预算仓储实现
/// </summary>
public class TimeBudgetRepository : ITimeBudgetRepository
{
    private readonly RecordTimeDbContext _context;
    private readonly bool _ownsContext;

    public TimeBudgetRepository(RecordTimeDbContext context, bool ownsContext = false)
    {
        _context = context;
        _ownsContext = ownsContext;
    }

    public async Task<List<TimeBudget>> GetEnabledBudgetsAsync()
    {
        return await _context.TimeBudgets
            .AsNoTracking()
            .Where(b => b.IsEnabled)
            .OrderBy(b => b.DisplayName)
            .ToListAsync();
    }

    public async Task<List<TimeBudget>> GetAllBudgetsAsync()
    {
        return await _context.TimeBudgets
            .AsNoTracking()
            .OrderBy(b => b.DisplayName)
            .ToListAsync();
    }

    public async Task<TimeBudget?> GetByIdAsync(int id)
    {
        return await _context.TimeBudgets.FindAsync(id);
    }

    public async Task<TimeBudget?> GetByProcessNameAsync(string processName)
    {
        return await _context.TimeBudgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.ProcessName == processName);
    }

    public async Task<TimeBudget?> GetByCategoryAsync(string category)
    {
        return await _context.TimeBudgets
            .AsNoTracking()
            .FirstOrDefaultAsync(b => b.Category == category);
    }

    public async Task<int> AddAsync(TimeBudget budget)
    {
        budget.CreatedAt = DateTime.Now;
        budget.UpdatedAt = DateTime.Now;
        _context.TimeBudgets.Add(budget);
        await _context.SaveChangesAsync();
        return budget.Id;
    }

    public async Task UpdateAsync(TimeBudget budget)
    {
        budget.UpdatedAt = DateTime.Now;
        _context.TimeBudgets.Update(budget);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var budget = await _context.TimeBudgets.FindAsync(id);
        if (budget != null)
        {
            _context.TimeBudgets.Remove(budget);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<DailyBudgetProgress?> GetTodayProgressAsync(int budgetId)
    {
        return await GetProgressAsync(budgetId, DateTime.Today);
    }

    public async Task<DailyBudgetProgress?> GetProgressAsync(int budgetId, DateTime date)
    {
        return await _context.DailyBudgetProgresses
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.TimeBudgetId == budgetId && p.Date.Date == date.Date);
    }

    public async Task<List<DailyBudgetProgress>> GetProgressHistoryAsync(int budgetId, int days)
    {
        var startDate = DateTime.Today.AddDays(-days);
        return await _context.DailyBudgetProgresses
            .AsNoTracking()
            .Where(p => p.TimeBudgetId == budgetId && p.Date >= startDate)
            .OrderByDescending(p => p.Date)
            .ToListAsync();
    }

    public async Task<DailyBudgetProgress> UpdateOrCreateProgressAsync(int budgetId, DateTime date, int actualMinutes)
    {
        var progress = await _context.DailyBudgetProgresses
            .FirstOrDefaultAsync(p => p.TimeBudgetId == budgetId && p.Date.Date == date.Date);

        if (progress == null)
        {
            // 获取当前预算的目标分钟数
            var budget = await _context.TimeBudgets.FindAsync(budgetId);
            if (budget == null)
                throw new InvalidOperationException($"TimeBudget with ID {budgetId} not found");

            progress = new DailyBudgetProgress
            {
                TimeBudgetId = budgetId,
                Date = date.Date,
                TargetMinutes = budget.TargetMinutes,
                ActualMinutes = actualMinutes,
                LastUpdated = DateTime.Now
            };
            _context.DailyBudgetProgresses.Add(progress);
        }
        else
        {
            progress.ActualMinutes = actualMinutes;
            progress.LastUpdated = DateTime.Now;

            // 检查是否达标
            var budget = await _context.TimeBudgets.FindAsync(budgetId);
            if (budget != null)
            {
                if (budget.Type == BudgetType.Maximum)
                {
                    progress.GoalReached = actualMinutes <= progress.TargetMinutes;
                }
                else
                {
                    progress.GoalReached = actualMinutes >= progress.TargetMinutes;
                }
            }
        }

        await _context.SaveChangesAsync();
        return progress;
    }

    public async Task MarkReminderSentAsync(int progressId)
    {
        var progress = await _context.DailyBudgetProgresses.FindAsync(progressId);
        if (progress != null)
        {
            progress.ReminderSent = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<GoalSuggestion>> GetUnprocessedSuggestionsAsync()
    {
        return await _context.GoalSuggestions
            .AsNoTracking()
            .Where(s => !s.IsProcessed && s.ExpiresAt > DateTime.Now)
            .OrderByDescending(s => s.Priority)
            .ThenByDescending(s => s.GeneratedAt)
            .ToListAsync();
    }

    public async Task<int> AddSuggestionAsync(GoalSuggestion suggestion)
    {
        suggestion.GeneratedAt = DateTime.Now;
        _context.GoalSuggestions.Add(suggestion);
        await _context.SaveChangesAsync();
        return suggestion.Id;
    }

    public async Task ProcessSuggestionAsync(int suggestionId, string result)
    {
        var suggestion = await _context.GoalSuggestions.FindAsync(suggestionId);
        if (suggestion != null)
        {
            suggestion.IsProcessed = true;
            suggestion.ProcessResult = result;
            await _context.SaveChangesAsync();
        }
    }

    public async Task CleanupExpiredSuggestionsAsync()
    {
        var expired = await _context.GoalSuggestions
            .Where(s => s.ExpiresAt < DateTime.Now || s.IsProcessed)
            .ToListAsync();

        _context.GoalSuggestions.RemoveRange(expired);
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        if (_ownsContext)
        {
            _context.Dispose();
        }
    }
}
