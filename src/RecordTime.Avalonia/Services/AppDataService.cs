using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;
using RecordTime.Data;

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
    private static AppDataService? _instance;
    public static AppDataService Instance => _instance ??= new AppDataService();

    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private readonly Dictionary<string, AppDataSnapshot> _snapshotCache = new();
    private const int MaxCacheSize = 7;

    private AppDataService() { }

    public async Task<AppDataSnapshot> GetSnapshotAsync(DateTime startDate, DateTime? endDate = null, bool forceRefresh = false)
    {
        var normalizedStart = startDate.Date;
        var normalizedEnd = (endDate?.Date ?? normalizedStart);
        if (normalizedEnd < normalizedStart)
        {
            (normalizedStart, normalizedEnd) = (normalizedEnd, normalizedStart);
        }

        var cacheKey = $"{normalizedStart:yyyyMMdd}-{normalizedEnd:yyyyMMdd}";

        await _loadLock.WaitAsync();
        try
        {
            if (!forceRefresh && _snapshotCache.TryGetValue(cacheKey, out var cached))
            {
                var includesToday = normalizedStart <= DateTime.Today && normalizedEnd >= DateTime.Today;
                if (!includesToday || (DateTime.Now - cached.LastUpdateTime).TotalSeconds <= 1)
                {
                    System.Diagnostics.Debug.WriteLine($"=== AppDataService: 使用缓存 [{cached.GetDebugInfo()}] ===");
                    return cached;
                }
            }

            System.Diagnostics.Debug.WriteLine($"=== AppDataService: 加载 {normalizedStart:yyyy-MM-dd} ~ {normalizedEnd:yyyy-MM-dd} 的数据... ===");
            var snapshot = await LoadSnapshotFromDatabaseAsync(normalizedStart, normalizedEnd);

            _snapshotCache[cacheKey] = snapshot;
            if (_snapshotCache.Count > MaxCacheSize)
            {
                var oldestKey = _snapshotCache
                    .OrderBy(kvp => kvp.Value.LastUpdateTime)
                    .First().Key;
                _snapshotCache.Remove(oldestKey);
            }

            System.Diagnostics.Debug.WriteLine($"=== AppDataService: 加载完成 [{snapshot.GetDebugInfo()}] ===");
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

            var sessions = await dbContext.Sessions
                .AsNoTracking() // 只读查询,不需要跟踪实体变化
                .Where(s => s.StartTime >= startDate && s.StartTime < endDate.AddDays(1))
                .ToListAsync();

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

            // 对于 EndTime == null 的会话,需要计算实际持续时长
            // 重要: 历史日期的会话不应使用当前时间,而应限制在当天结束时
            var now = DateTime.Now;
            var queryEndBoundary = endDate.AddDays(1); // 查询日期的次日 00:00

            foreach (var session in sessions)
            {
                if (session.EndTime == null)
                {
                    // 对于未结束的会话:
                    // - 如果是今天的会话: 使用当前时间
                    // - 如果是历史日期的会话: 使用当天的结束时间 (次日 00:00)
                    var effectiveEndTime = (session.StartTime.Date < DateTime.Today)
                        ? queryEndBoundary  // 历史会话: 限制在查询范围的结束边界
                        : now;              // 今天的会话: 使用当前时间

                    session.DurationSeconds = (int)(effectiveEndTime - session.StartTime).TotalSeconds;
                }
            }

            var totalSeconds = sessions.Sum(s => s.DurationSeconds);

            var allApps = sessions
                .GroupBy(s => string.IsNullOrEmpty(s.DisplayName) ? s.ProcessName : s.DisplayName)
                .Select(g => new AppDataItem
                {
                    AppName = g.Key,
                    ProcessName = g.First().ProcessName,
                    Category = g.First().Category ?? "未分类",
                    TotalDuration = TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds)),
                    SessionCount = g.Count(),
                    FirstUsed = g.Min(s => s.StartTime),
                    LastUsed = g.Max(s => s.EndTime ?? s.StartTime),
                    TotalPercentage = totalSeconds == 0 ? 0 : (double)g.Sum(s => s.DurationSeconds) / totalSeconds * 100
                })
                .OrderByDescending(x => x.TotalDuration)
                .ToList();

            var topApps = allApps.Take(10).ToList();

            System.Diagnostics.Debug.WriteLine($"  查询到 {sessions.Count} 个会话，{allApps.Count} 个应用");
            foreach (var (app, index) in topApps.Select((value, index) => (value, index)))
            {
                System.Diagnostics.Debug.WriteLine($"  #{index + 1}: {app.AppName} - {app.TotalDuration.TotalSeconds:F0}s");
            }

            return new AppDataSnapshot
            {
                StartDate = startDate,
                EndDate = endDate,
                LastUpdateTime = DateTime.Now,
                AllApps = allApps.AsReadOnly(),
                TopApps = topApps.AsReadOnly(),
                TotalSeconds = totalSeconds,
                SessionCount = sessions.Count
            };
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"AppDataService 加载失败: {ex.Message}");
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
    public string SessionCountText => $"{SessionCount} 次";
    public string PercentageText => $"{TotalPercentage:F1}%";
    public string FirstUsedText => FirstUsed.ToString("HH:mm:ss");
    public string LastUsedText => LastUsed.ToString("HH:mm:ss");
}
