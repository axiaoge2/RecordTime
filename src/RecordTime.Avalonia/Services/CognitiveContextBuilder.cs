using System;
using System.Threading.Tasks;
using RecordTime.Core.Models;
using RecordTime.Core.Models.AICoach;
using RecordTime.Core.Repositories;
using RecordTime.Core.Services;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 认知上下文构建器 - 从数据库和系统状态构建 AI Coach 所需的上下文
/// </summary>
public class CognitiveContextBuilder
{
    private readonly Func<ISessionRepository> _repositoryFactory;
    private readonly IWindowMonitor? _windowMonitor;
    private readonly IActivityDetector? _activityDetector;

    public CognitiveContextBuilder(
        Func<ISessionRepository> repositoryFactory,
        IWindowMonitor? windowMonitor = null,
        IActivityDetector? activityDetector = null)
    {
        _repositoryFactory = repositoryFactory;
        _windowMonitor = windowMonitor;
        _activityDetector = activityDetector;
    }

    /// <summary>
    /// 构建当前的认知上下文
    /// </summary>
    public async Task<CognitiveContext> BuildContextAsync()
    {
        var context = new CognitiveContext
        {
            CurrentTime = DateTime.Now
        };

        try
        {
            // 构建今日摘要
            context.TodaySummary = await BuildTodaySummaryAsync();

            // 构建当前会话信息
            context.CurrentSession = await BuildCurrentSessionAsync();

            // 构建最近模式
            context.RecentPattern = BuildRecentPattern();

            // TODO: 加载个人参数（后续实现参数学习后添加）
            // context.PersonalParams = await LoadUserParametersAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "构建认知上下文时发生错误");
        }

        return context;
    }

    /// <summary>
    /// 构建今日摘要
    /// </summary>
    private async Task<TodaySummary> BuildTodaySummaryAsync()
    {
        var summary = new TodaySummary();

        try
        {
            using var repository = _repositoryFactory();
            var today = DateTime.Today;
            var sessions = await repository.GetSessionsByDateAsync(today);

            foreach (var session in sessions)
            {
                var duration = session.DurationSeconds / 60; // 转换为分钟

                // 累计专注/空闲时间
                if (session.ActivityType == ActivityType.Idle)
                {
                    summary.TotalIdleMinutes += duration;
                }
                else
                {
                    summary.TotalFocusMinutes += duration;
                }

                // 按分类统计
                var category = session.Category ?? "其他";
                if (!summary.ByCategory.ContainsKey(category))
                    summary.ByCategory[category] = 0;
                summary.ByCategory[category] += duration;

                // 按应用统计
                var appName = session.DisplayName ?? session.ProcessName;
                if (!summary.ByApp.ContainsKey(appName))
                    summary.ByApp[appName] = 0;
                summary.ByApp[appName] += duration;
            }

            // 计算应用切换次数（基于会话数量）
            summary.AppSwitchCount = Math.Max(0, sessions.Count - 1);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "构建今日摘要时发生错误");
        }

        return summary;
    }

    /// <summary>
    /// 构建当前会话信息
    /// </summary>
    private async Task<CurrentSessionInfo?> BuildCurrentSessionAsync()
    {
        try
        {
            using var repository = _repositoryFactory();
            var activeSession = await repository.GetActiveSessionAsync();

            if (activeSession == null)
                return null;

            return new CurrentSessionInfo
            {
                App = activeSession.DisplayName ?? activeSession.ProcessName,
                Category = activeSession.Category ?? "其他",
                StartTime = activeSession.StartTime,
                DurationMinutes = (int)(DateTime.Now - activeSession.StartTime).TotalMinutes,
                ActivityType = activeSession.ActivityType
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "构建当前会话信息时发生错误");
            return null;
        }
    }

    /// <summary>
    /// 构建最近模式
    /// </summary>
    private RecentPattern BuildRecentPattern()
    {
        var pattern = new RecentPattern();

        try
        {
            if (_windowMonitor != null)
            {
                var switchStats = _windowMonitor.GetSwitchStats();
                pattern.SwitchesLast5Min = switchStats.SwitchesLast5Min;
                pattern.IsDeepFocus = switchStats.IsDeepFocus;
                pattern.IsFragmented = switchStats.IsFragmented;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "构建最近模式时发生错误");
        }

        return pattern;
    }
}
