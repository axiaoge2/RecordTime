namespace RecordTime.Core.Services;

/// <summary>
/// 计算会话在指定时间范围内的有效时长，并处理进行中的会话。
/// </summary>
public static class SessionDurationCalculator
{
    /// <summary>
    /// 获取会话在范围结束时间截断后的实际结束时间。
    /// </summary>
    public static DateTime GetClampedEndTime(DateTime? endTime, DateTime rangeEnd, DateTime now)
    {
        var effectiveEnd = endTime ?? now;
        return effectiveEnd > rangeEnd ? rangeEnd : effectiveEnd;
    }

    /// <summary>
    /// 获取会话在指定日期范围内的有效时长（秒）。
    /// </summary>
    public static int GetDurationSeconds(
        DateTime startTime,
        DateTime? endTime,
        DateTime rangeStart,
        DateTime rangeEnd,
        DateTime now)
    {
        var effectiveStart = startTime < rangeStart ? rangeStart : startTime;
        var effectiveEnd = GetClampedEndTime(endTime, rangeEnd, now);
        return effectiveEnd <= effectiveStart ? 0 : (int)(effectiveEnd - effectiveStart).TotalSeconds;
    }
}
