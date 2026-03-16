using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;
using RecordTime.Data;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 应用数据快照（不可变）
/// </summary>
public class AppDataSnapshot
{
    public DateTime StartDate { get; init; }
    public DateTime EndDate { get; init; }
    public DateTime LastUpdateTime { get; init; }
    public IReadOnlyList<AppDataItem> AllApps { get; init; } = Array.Empty<AppDataItem>();
    public IReadOnlyList<AppDataItem> TopApps { get; init; } = Array.Empty<AppDataItem>();
    public int TotalSeconds { get; init; }
    public int SessionCount { get; init; }

    public string GetDebugInfo()
    {
        var range = StartDate == EndDate
            ? StartDate.ToString("yyyy-MM-dd")
            : $"{StartDate:yyyy-MM-dd}~{EndDate:yyyy-MM-dd}";
        return $"Range={range}, Apps={AllApps.Count}, Top10={TopApps.Count}, Updated={LastUpdateTime:HH:mm:ss}";
    }
}

/// <summary>
/// 应用数据服务 - 提供统一、线程安全的数据源
/// </summary>
public class AppDataService
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly Dictionary<string, AppDataSnapshot> _snapshotCache = new();
    private const int MaxCacheSize = 7;

    public AppDataService() { }

    public async Task<AppDataSnapshot> GetSnapshotAsync(DateTime startDate, DateTime? endDate = null, bool forceRefresh = false)
    {
        var normalizedStart = startDate.Date;
        var normalizedEnd = (endDate?.Date ?? normalizedStart);
        if (normalizedEnd < normalizedStart)
        {
            (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);
        }

        var cacheKey = $"{normalizedStart:yyyyMMdd}-{normalizedEnd:yyyyMMdd}";

        await _loadLock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!forceRefresh && _snapshotCache.TryGetValue(cacheKey, out var cached))
            {
                var includesToday = normalizedStart <= DateTime.Today && normalizedEnd >= DateTime.Today;
                if (!includesToday || (DateTime.Now - cached.LastUpdateTime).TotalSeconds <= 1)
                {
                    Log.Debug("AppDataService: 使用缓存 [{DebugInfo}]", cached.GetDebugInfo());
                    return cached;
                }
            }

            Log.Debug("AppDataService: 加载 {Start} ~ {End} 的数据...", normalizedStart.ToString("yyyy-MM-dd"), normalizedEnd.ToString("yyyy-MM-dd"));
            var snapshot = await LoadSnapshotFromDatabaseAsync(normalizedStart, normalizedEnd).ConfigureAwait(false);

            _snapshotCache[cacheKey] = snapshot;
            if (_snapshotCache.Count > MaxCacheSize)
            {
                var oldestKey = _snapshotCache
                    .OrderBy(kvp => kvp.Value.LastUpdateTime)
                    .First().Key;
                _snapshotCache.Remove(oldestKey);
            }

            Log.Debug("AppDataService: 加载完成 [{DebugInfo}]", snapshot.GetDebugInfo());
            return snapshot;
        }
        finally
        {
            _loadLock.Release();
        }
    }

    private async Task<AppDataSnapshot> LoadSnapshotFromDatabaseAsync(DateTime startDate, DateTime endDate)
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var rangeStart = startDate.Date;
            var rangeEnd = endDate.Date.AddDays(1); // 包含结束日期当天

            var sessions = await dbContext.Sessions
                .AsNoTracking()
                .Where(s => s.EndTime != null && s.StartTime < rangeEnd && s.EndTime >= rangeStart)
                .Select(s => new AppSession
                {
                    Id = s.Id,
                    ProcessName = s.ProcessName,
                    DisplayName = s.DisplayName,
                    Category = s.Category,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    DurationSeconds = s.DurationSeconds
                })
                .ToListAsync().ConfigureAwait(false);

            if (sessions.Count == 0)
            {
                return new AppDataSnapshot
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    LastUpdateTime = DateTime.Now,
                    AllApps = Array.Empty<AppDataItem>(),
                    TopApps = Array.Empty<AppDataItem>(),
                    TotalSeconds = 0,
                    SessionCount = 0
                };
            }

            // 计算每个会话在查询范围内的实际时长
            var adjustedSessions = new List<(AppSession session, int effectiveDuration)>();

            foreach (var session in sessions)
            {
                // 计算会话在日期范围内的实际开始和结束时间
                var effectiveStart = session.StartTime < rangeStart ? rangeStart : session.StartTime;
                var effectiveEnd = session.EndTime!.Value > rangeEnd ? rangeEnd : session.EndTime.Value;

                // 计算在范围内的时长(秒)
                var durationInRange = (int)(effectiveEnd - effectiveStart).TotalSeconds;

                if (durationInRange > 0)
                {
                    adjustedSessions.Add((session, durationInRange));
                }
            }

            var totalSeconds = adjustedSessions.Sum(x => x.effectiveDuration);

            var allApps = adjustedSessions
                .GroupBy(x => string.IsNullOrEmpty(x.session.DisplayName) ? x.session.ProcessName : x.session.DisplayName)
                .Select(g => new AppDataItem
                {
                    AppName = g.Key,
                    ProcessName = g.First().session.ProcessName,
                    Category = g.First().session.Category ?? "未分类",
                    TotalDuration = TimeSpan.FromSeconds(g.Sum(x => x.effectiveDuration)),
                    SessionCount = g.Count(),
                    FirstUsed = g.Min(x => x.session.StartTime < rangeStart ? rangeStart : x.session.StartTime),
                    LastUsed = g.Max(x => x.session.EndTime > rangeEnd ? rangeEnd : x.session.EndTime!.Value),
                    TotalPercentage = totalSeconds == 0 ? 0 : (double)g.Sum(x => x.effectiveDuration) / totalSeconds * 100
                })
                .OrderByDescending(x => x.TotalDuration)
                .ToList();

            var topApps = allApps.Take(10).ToList();

            Log.Debug("AppDataService: 查询到 {SessionCount} 个会话，{AppCount} 个应用", sessions.Count, allApps.Count);

            return new AppDataSnapshot
            {
                StartDate = startDate,
                EndDate = endDate,
                LastUpdateTime = DateTime.Now,
                AllApps = allApps.AsReadOnly(),
                TopApps = topApps.AsReadOnly(),
                TotalSeconds = totalSeconds,
                SessionCount = adjustedSessions.Count
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AppDataService 加载失败");
            return new AppDataSnapshot
            {
                StartDate = startDate,
                EndDate = endDate,
                LastUpdateTime = DateTime.Now,
                AllApps = Array.Empty<AppDataItem>(),
                TopApps = Array.Empty<AppDataItem>(),
                TotalSeconds = 0,
                SessionCount = 0
            };
        }
    }

    /// <summary>
    /// Get the last session end time for a given date (for "updated at" display).
    /// </summary>
    public async Task<DateTime?> GetLastSessionEndTimeAsync(DateTime date)
    {
        try
        {
            await using var db = new RecordTimeDbContext();
            var dateStart = date.Date;
            return await db.Sessions
                .AsNoTracking()
                .Where(s => s.StartTime >= dateStart && s.StartTime < dateStart.AddDays(1) && s.EndTime != null)
                .OrderByDescending(s => s.EndTime)
                .Select(s => s.EndTime)
                .FirstOrDefaultAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取最后会话时间失败");
            return null;
        }
    }

    /// <summary>
    /// Get the count of distinct activity types for a given date.
    /// </summary>
    public async Task<int> GetActivityTypeCountAsync(DateTime date)
    {
        try
        {
            await using var db = new RecordTimeDbContext();
            var targetDate = date.Date;
            return await db.Sessions
                .AsNoTracking()
                .Where(s => s.StartTime >= targetDate && s.StartTime < targetDate.AddDays(1))
                .Select(s => s.ActivityType)
                .Distinct()
                .CountAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取活动类型数量失败");
            return 0;
        }
    }

    public void InvalidateCache(DateTime date)
    {
        var target = date.Date;
        var keys = _snapshotCache
            .Where(kvp => target >= kvp.Value.StartDate && target <= kvp.Value.EndDate)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in keys)
        {
            _snapshotCache.Remove(key);
        }
    }

    public void ClearCache() => _snapshotCache.Clear();
}

public class AppDataItem
{
    public string AppName { get; set; } = string.Empty;
    public string ProcessName { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public TimeSpan TotalDuration { get; set; }
    public int SessionCount { get; set; }
    public DateTime FirstUsed { get; set; }
    public DateTime LastUsed { get; set; }
    public double TotalPercentage { get; set; }

    public string DurationText => $"{(int)TotalDuration.TotalHours:D2}:{TotalDuration.Minutes:D2}:{TotalDuration.Seconds:D2}";
    public string SessionCountText => $"{SessionCount}{RecordTime.Avalonia.Resources.Strings.StringResources.Current.UsageCountSuffix}";
    public string PercentageText => $"{TotalPercentage:F1}%";
    public string FirstUsedText => FirstUsed.ToString("HH:mm:ss");
    public string LastUsedText => LastUsed.ToString("HH:mm:ss");
}
