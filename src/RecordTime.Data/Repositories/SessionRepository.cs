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
