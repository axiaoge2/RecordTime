using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;

namespace RecordTime.Data.Repositories;

/// <summary>
/// 会话数据仓储实现
/// </summary>
public class SessionRepository : ISessionRepository, IDisposable
{
    private readonly RecordTimeDbContext _context;
    private readonly bool _ownsContext;
    private bool _disposed = false;

    public SessionRepository(RecordTimeDbContext context, bool ownsContext = false)
    {
        _context = context;
        _ownsContext = ownsContext;
    }

    public async Task<int> AddSessionAsync(AppSession session)
    {
        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();
        return session.Id;
    }

    public async Task UpdateSessionAsync(AppSession session)
    {
        _context.Sessions.Update(session);
        await _context.SaveChangesAsync();
    }

    public async Task EndSessionAsync(int sessionId, DateTime endTime)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session != null)
        {
            session.EndTime = endTime;
            session.DurationSeconds = (int)(endTime - session.StartTime).TotalSeconds;
            await _context.SaveChangesAsync();
        }
    }

    // Phase 1 Task 1.2: 更新会话心跳时间
    public async Task UpdateSessionHeartbeatAsync(int sessionId, DateTime heartbeatTime)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session != null)
        {
            session.LastHeartbeat = heartbeatTime;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<AppSession?> GetSessionByIdAsync(int sessionId)
    {
        return await _context.Sessions.FindAsync(sessionId);
    }

    public async Task<List<AppSession>> GetSessionsByDateAsync(DateTime date)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await _context.Sessions
            .AsNoTracking() // 只读查询,不需要跟踪实体变化
            .Where(s => s.StartTime >= startOfDay && s.StartTime < endOfDay)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<List<AppSession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Sessions
            .AsNoTracking() // 只读查询,不需要跟踪实体变化
            .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
            .OrderBy(s => s.StartTime)
            .ToListAsync();
    }

    public async Task<Dictionary<string, int>> GetAppUsageStatsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Sessions
            .AsNoTracking() // 只读查询,不需要跟踪实体变化
            .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
            .GroupBy(s => s.ProcessName)
            .Select(g => new { App = g.Key, TotalSeconds = g.Sum(s => s.DurationSeconds) })
            .ToDictionaryAsync(x => x.App, x => x.TotalSeconds);
    }

    public async Task<Dictionary<ActivityType, int>> GetActivityStatsAsync(DateTime startDate, DateTime endDate)
    {
        return await _context.Sessions
            .AsNoTracking() // 只读查询,不需要跟踪实体变化
            .Where(s => s.StartTime >= startDate && s.StartTime <= endDate)
            .GroupBy(s => s.ActivityType)
            .Select(g => new { Type = g.Key, TotalSeconds = g.Sum(s => s.DurationSeconds) })
            .ToDictionaryAsync(x => x.Type, x => x.TotalSeconds);
    }

    public async Task<AppSession?> GetActiveSessionAsync()
    {
        return await _context.Sessions
            .AsNoTracking() // 只读查询,不需要跟踪实体变化
            .Where(s => s.EndTime == null)
            .OrderByDescending(s => s.StartTime)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// 获取指定日期范围内的每日使用趋势(正确处理跨日会话) - 重载版本,接受起止日期
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>每日使用统计列表</returns>
    public async Task<List<DailyUsage>> GetWeeklyTrendAsync(DateTime startDate, DateTime endDate)
    {
        var rangeStart = startDate.Date;
        var rangeEnd = endDate.Date.AddDays(1);
        var days = (int)(endDate.Date - startDate.Date).TotalDays + 1;

        // 获取所有与日期范围有交集的会话
        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.EndTime != null && s.StartTime < rangeEnd && s.EndTime >= rangeStart)
            .ToListAsync();

        // 为每一天初始化使用时长字典
        var dailySeconds = Enumerable.Range(0, days)
            .Select(offset => rangeStart.AddDays(offset))
            .ToDictionary(date => date, _ => 0.0);

        // 遍历每个会话,将时长分配到各个日期
        foreach (var session in sessions)
        {
            var sessionStart = session.StartTime;
            var sessionEnd = session.EndTime!.Value;

            // 遍历会话跨越的每一天
            var currentDay = sessionStart.Date;
            while (currentDay < sessionEnd.Date.AddDays(1) && currentDay < rangeEnd)
            {
                if (currentDay >= rangeStart && currentDay < rangeEnd)
                {
                    // 计算会话在当前日期的有效时间段
                    var dayStart = currentDay;
                    var dayEnd = currentDay.AddDays(1);

                    var effectiveStart = sessionStart > dayStart ? sessionStart : dayStart;
                    var effectiveEnd = sessionEnd < dayEnd ? sessionEnd : dayEnd;

                    // 计算当天的时长(秒)
                    var secondsInDay = (effectiveEnd - effectiveStart).TotalSeconds;

                    if (secondsInDay > 0 && dailySeconds.ContainsKey(currentDay))
                    {
                        dailySeconds[currentDay] += secondsInDay;
                    }
                }

                currentDay = currentDay.AddDays(1);
            }
        }

        // 转换为 DailyUsage 列表
        var result = dailySeconds
            .OrderBy(kv => kv.Key)
            .Select(kv => new DailyUsage
            {
                Date = kv.Key,
                TotalDuration = TimeSpan.FromSeconds(kv.Value)
            })
            .ToList();

        return result;
    }

    /// <summary>
    /// 获取指定日期范围内的每日使用趋势(正确处理跨日会话) - 兼容旧版本,通过天数计算
    /// </summary>
    /// <param name="endDate">结束日期</param>
    /// <param name="days">天数（默认7天）</param>
    /// <returns>每日使用统计列表</returns>
    public async Task<List<DailyUsage>> GetWeeklyTrendAsync(DateTime endDate, int days = 7)
    {
        var startDate = endDate.Date.AddDays(-(days - 1));
        var rangeEnd = endDate.Date.AddDays(1);

        // 获取所有与日期范围有交集的会话
        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.EndTime != null && s.StartTime < rangeEnd && s.EndTime >= startDate)
            .ToListAsync();

        // 为每一天初始化使用时长字典
        var dailySeconds = Enumerable.Range(0, days)
            .Select(offset => startDate.AddDays(offset))
            .ToDictionary(date => date, _ => 0.0);

        // 遍历每个会话,将时长分配到各个日期
        foreach (var session in sessions)
        {
            var sessionStart = session.StartTime;
            var sessionEnd = session.EndTime!.Value;

            // 遍历会话跨越的每一天
            var currentDay = sessionStart.Date;
            while (currentDay < sessionEnd.Date.AddDays(1) && currentDay < rangeEnd)
            {
                if (currentDay >= startDate && currentDay < rangeEnd)
                {
                    // 计算会话在当前日期的有效时间段
                    var dayStart = currentDay;
                    var dayEnd = currentDay.AddDays(1);

                    var effectiveStart = sessionStart > dayStart ? sessionStart : dayStart;
                    var effectiveEnd = sessionEnd < dayEnd ? sessionEnd : dayEnd;

                    // 计算当天的时长(秒)
                    var secondsInDay = (effectiveEnd - effectiveStart).TotalSeconds;

                    if (secondsInDay > 0 && dailySeconds.ContainsKey(currentDay))
                    {
                        dailySeconds[currentDay] += secondsInDay;
                    }
                }

                currentDay = currentDay.AddDays(1);
            }
        }

        // 转换为 DailyUsage 列表
        var result = dailySeconds
            .OrderBy(kv => kv.Key)
            .Select(kv => new DailyUsage
            {
                Date = kv.Key,
                TotalDuration = TimeSpan.FromSeconds(kv.Value)
            })
            .ToList();

        return result;
    }

    /// <summary>
    /// 获取指定日期范围的活动类型分布(正确处理跨日会话)
    /// </summary>
    /// <param name="startDate">开始日期</param>
    /// <param name="endDate">结束日期</param>
    /// <returns>活动类型和时长的字典</returns>
    public async Task<Dictionary<ActivityType, TimeSpan>> GetActivityDistributionAsync(DateTime startDate, DateTime endDate)
    {
        var rangeStart = startDate.Date;
        var rangeEnd = endDate.Date.AddDays(1); // 包含结束日期当天

        // 获取所有与日期范围有交集的会话
        var sessions = await _context.Sessions
            .AsNoTracking()
            .Where(s => s.EndTime != null && s.StartTime < rangeEnd && s.EndTime >= rangeStart)
            .ToListAsync();

        // 计算每个活动类型在日期范围内的实际时长
        var activityTotals = new Dictionary<ActivityType, double>();

        foreach (var session in sessions)
        {
            // 计算会话在日期范围内的实际开始和结束时间
            var effectiveStart = session.StartTime < rangeStart ? rangeStart : session.StartTime;
            var effectiveEnd = session.EndTime!.Value > rangeEnd ? rangeEnd : session.EndTime.Value;

            // 计算在范围内的时长(秒)
            var durationInRange = (effectiveEnd - effectiveStart).TotalSeconds;

            if (durationInRange > 0)
            {
                if (!activityTotals.ContainsKey(session.ActivityType))
                {
                    activityTotals[session.ActivityType] = 0;
                }
                activityTotals[session.ActivityType] += durationInRange;
            }
        }

        return activityTotals.ToDictionary(
            x => x.Key,
            x => TimeSpan.FromSeconds(x.Value)
        );
    }

    /// <summary>
    /// 获取指定日期的活动类型分布(兼容旧版本,内部调用日期范围方法)
    /// </summary>
    /// <param name="date">日期</param>
    /// <returns>活动类型和时长的字典</returns>
    public async Task<Dictionary<ActivityType, TimeSpan>> GetActivityDistributionAsync(DateTime date)
    {
        return await GetActivityDistributionAsync(date, date);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing && _ownsContext)
        {
            _context?.Dispose();
        }

        _disposed = true;
    }
}
