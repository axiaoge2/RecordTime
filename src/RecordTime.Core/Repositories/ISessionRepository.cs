using RecordTime.Core.Models;

namespace RecordTime.Core.Repositories;

/// <summary>
/// 会话数据仓储接口
/// </summary>
public interface ISessionRepository : IDisposable
{
    /// <summary>
    /// 添加新会话
    /// </summary>
    Task<int> AddSessionAsync(AppSession session);

    /// <summary>
    /// 更新会话
    /// </summary>
    Task UpdateSessionAsync(AppSession session);

    /// <summary>
    /// 结束会话
    /// </summary>
    Task EndSessionAsync(int sessionId, DateTime endTime);

    /// <summary>
    /// 获取会话
    /// </summary>
    Task<AppSession?> GetSessionByIdAsync(int sessionId);

    /// <summary>
    /// 获取指定日期的所有会话
    /// </summary>
    Task<List<AppSession>> GetSessionsByDateAsync(DateTime date);

    /// <summary>
    /// 获取日期范围内的会话
    /// </summary>
    Task<List<AppSession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取应用使用统计
    /// </summary>
    Task<Dictionary<string, int>> GetAppUsageStatsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取活动类型统计
    /// </summary>
    Task<Dictionary<ActivityType, int>> GetActivityStatsAsync(DateTime startDate, DateTime endDate);

    /// <summary>
    /// 获取当前活动的会话
    /// </summary>
    Task<AppSession?> GetActiveSessionAsync();
}
