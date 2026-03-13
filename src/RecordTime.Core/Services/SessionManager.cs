using RecordTime.Core.Models;
using RecordTime.Core.Repositories;
using Serilog;

namespace RecordTime.Core.Services;

/// <summary>
/// 会话管理器 - 整合所有监控服务
/// </summary>
public class SessionManager : IDisposable
{
    private readonly IWindowMonitor _windowMonitor;
    private readonly IInputMonitor _inputMonitor;
    private readonly IMediaDetector _mediaDetector;
    private readonly IActivityDetector _activityDetector;
    private readonly Func<ISessionRepository> _repositoryFactory;
    private readonly int _idleTimeoutSeconds;

    private int? _currentSessionId;
    private readonly SemaphoreSlim _sessionLock = new(1, 1);
    private volatile bool _isRunning;
    
    // 性能优化: 复用 SHA256 实例
    private readonly System.Security.Cryptography.SHA256 _sha256 = System.Security.Cryptography.SHA256.Create();

    // Phase 1 Task 1.2: 会话心跳机制
    private System.Threading.Timer? _heartbeatTimer;
    private readonly int _heartbeatIntervalSeconds = 30; // 每30秒更新一次心跳

    // 空闲检查机制
    private System.Threading.Timer? _idleCheckTimer;
    private readonly int _idleCheckIntervalSeconds = 120; // 默认 2 分钟检查一次
    private volatile bool _sessionPausedDueToIdle = false;

    public event EventHandler<AppSession>? SessionStarted;
    public event EventHandler<AppSession>? SessionEnded;

    public SessionManager(
        IWindowMonitor windowMonitor,
        IInputMonitor inputMonitor,
        IMediaDetector mediaDetector,
        IActivityDetector activityDetector,
        Func<ISessionRepository> repositoryFactory,
        int idleTimeoutSeconds = 300) // 默认 5 分钟
    {
        _windowMonitor = windowMonitor;
        _inputMonitor = inputMonitor;
        _mediaDetector = mediaDetector;
        _activityDetector = activityDetector;
        _repositoryFactory = repositoryFactory;
        _idleTimeoutSeconds = idleTimeoutSeconds;
    }

    /// <summary>
    /// 启动会话管理
    /// </summary>
    public void Start()
    {
        if (_isRunning)
            return;

        _isRunning = true;

        // 启动所有监控服务
        _windowMonitor.Start();
        _inputMonitor.Start();
        _mediaDetector.Start();

        // 订阅窗口切换事件
        _windowMonitor.WindowFocusChanged += OnWindowFocusChanged;

        // 订阅用户活动事件（用于会话恢复）
        _inputMonitor.UserActivityDetected += OnUserActivityDetected;

        // 启动空闲检查定时器
        StartIdleCheckTimer();

        Log.Information("SessionManager 已启动");
    }

    /// <summary>
    /// 停止会话管理
    /// </summary>
    public async Task StopAsync()
    {
        if (!_isRunning)
            return;

        _isRunning = false;

        // 结束当前会话
        if (_currentSessionId != null)
        {
            await EndCurrentSessionAsync();
        }

        // 停止所有定时器
        StopIdleCheckTimer();

        // 取消订阅
        _windowMonitor.WindowFocusChanged -= OnWindowFocusChanged;
        _inputMonitor.UserActivityDetected -= OnUserActivityDetected;

        // 停止所有监控服务
        _windowMonitor.Stop();
        _inputMonitor.Stop();
        _mediaDetector.Stop();

        Log.Information("SessionManager 已停止");
    }

    private void OnWindowFocusChanged(object? sender, WindowInfo window)
    {
        // 不等待异步操作,但确保异常被捕获和记录
        _ = HandleWindowFocusChangedAsync(window);
    }

    private async Task HandleWindowFocusChangedAsync(WindowInfo window)
    {
        try
        {
            Log.Debug("收到窗口焦点变化事件: {ProcessName}", window.ProcessName);

            if (!_isRunning)
            {
                Log.Debug("SessionManager 未运行,忽略窗口焦点变化事件");
                return;
            }

            await _sessionLock.WaitAsync();
            try
            {
                Log.Debug("已获取会话锁,开始处理会话");

                // 收集系统状态
                var systemState = CollectSystemState(window);

                // 判定活动类型
                var activityType = _activityDetector.DetermineActivity(window, systemState);
                var category = _activityDetector.GetAppCategory(window.ProcessName);

                Log.Debug("活动分析完成: 类型={ActivityType}, 分类={Category}", activityType, category);

                // 结束上一个会话
                if (_currentSessionId != null)
                {
                    Log.Debug("结束上一个会话: SessionId={SessionId}", _currentSessionId);
                    await EndCurrentSessionAsync();
                }

                // 创建新会话
                Log.Debug("创建新会话: ProcessName={ProcessName}", window.ProcessName);
                await StartNewSessionAsync(window, activityType, category);
            }
            finally
            {
                _sessionLock.Release();
                Log.Debug("已释放会话锁");
            }
        }
        catch (Exception ex)
        {
            // 记录异常但不让它传播导致应用崩溃
            Log.Error(ex, "处理窗口焦点变化时发生错误: ProcessName={ProcessName}", window.ProcessName);
        }
    }

    private SystemState CollectSystemState(WindowInfo window)
    {
        // 性能优化: 一次性获取所有输入统计数据，减少锁竞争
        var inputStats = _inputMonitor.GetInputStats(30);

        var isMediaPlaying = _mediaDetector.IsMediaPlaying();
        var isVideoPlayer = _mediaDetector.IsVideoPlayer(window.ProcessName);
        var isBrowser = _mediaDetector.IsBrowser(window.ProcessName);
        var hasAudio = _mediaDetector.HasAudioActivity();

        // 检测后台媒体播放状态
        var isMusicPlayerRunning = _mediaDetector.IsMusicPlayerRunning();
        var isVideoPlayerRunning = _mediaDetector.IsVideoPlayerRunning();

        // 获取应用切换频率统计
        var switchStats = _windowMonitor.GetSwitchStats();

        // 修复浏览器视频检测：
        // 只有当浏览器是当前窗口，且有媒体播放，
        // 但没有独立的视频播放器或音乐播放器在后台运行时，才认为是浏览器视频
        var browserVideoPlaying = isBrowser && isMediaPlaying &&
                                  !isVideoPlayerRunning && !isMusicPlayerRunning;

        return new SystemState
        {
            MediaSessionPlaying = isMediaPlaying,
            IsVideoApp = isVideoPlayer,
            AudioActive = hasAudio,
            IsBrowser = isBrowser,
            BrowserVideoPlaying = browserVideoPlaying,
            WindowFocused = true,
            SystemIdle = inputStats.IdleTimeSeconds > _idleTimeoutSeconds,
            KeyboardActivityLast30s = inputStats.KeyboardActivityCount,
            MouseClicksLast30s = inputStats.MouseClickCount,
            ScrollCountLast30s = inputStats.ScrollCount,
            AppSwitchCountLast5Min = switchStats.SwitchesLast5Min,
            IsAttentionFragmented = switchStats.IsFragmented,
            IsDeepFocus = switchStats.IsDeepFocus,
            FrequentInput = inputStats.KeyboardActivityCount > 20 || inputStats.MouseClickCount > 10,
            HasBackgroundMusic = isMusicPlayerRunning && !_mediaDetector.IsMusicPlayer(window.ProcessName),
            HasBackgroundVideoPlayer = isVideoPlayerRunning && !isVideoPlayer,
            HighGpuUsage = false // 简化实现，实际需要检测GPU使用率
        };
    }

    private async Task StartNewSessionAsync(WindowInfo window, ActivityType activityType, string category)
    {
        using var repository = _repositoryFactory();

        // 获取友好的显示名称
        var friendlyName = AppNameResolver.GetFriendlyName(window.ProcessName);

        var now = DateTime.Now;
        var session = new AppSession
        {
            ProcessName = window.ProcessName,
            DisplayName = friendlyName,
            ActivityType = activityType,
            Category = category,
            StartTime = now,
            Confidence = CalculateConfidence(activityType),
            WindowTitleHash = HashWindowTitle(window.WindowTitle),
            LastHeartbeat = now // Phase 1 Task 1.2: 初始化心跳时间
        };

        _currentSessionId = await repository.AddSessionAsync(session);
        session.Id = _currentSessionId.Value;

        // Phase 1 Task 1.2: 启动心跳定时器
        StartHeartbeatTimer();

        SessionStarted?.Invoke(this, session);

        Log.Information("会话已开始: {DisplayName} [{ProcessName}] ({ActivityType})", friendlyName, window.ProcessName, activityType);
    }

    private string HashWindowTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        // 注意：此方法在 _sessionLock 保护下调用，无需额外锁
        var bytes = System.Text.Encoding.UTF8.GetBytes(title);
        var hash = _sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private async Task EndCurrentSessionAsync()
    {
        if (_currentSessionId == null)
            return;

        // Phase 1 Task 1.2: 停止心跳定时器
        StopHeartbeatTimer();

        using var repository = _repositoryFactory();

        await repository.EndSessionAsync(_currentSessionId.Value, DateTime.Now);

        var session = await repository.GetSessionByIdAsync(_currentSessionId.Value);
        if (session != null)
        {
            SessionEnded?.Invoke(this, session);
        }

        Log.Debug("会话已结束: SessionId={SessionId}", _currentSessionId);
        _currentSessionId = null;
    }

    private int CalculateConfidence(ActivityType activityType)
    {
        return activityType switch
        {
            ActivityType.Video => 90,
            ActivityType.Gaming => 85,
            ActivityType.Meeting => 85, // 会议应用 + 音频活动，可信度较高
            ActivityType.ActiveTyping => 80,
            ActivityType.Reading => 75, // 高滚轮 + 低键盘活动，可信度中等
            ActivityType.PassiveBrowsing => 60,
            ActivityType.Idle => 100,
            _ => 50
        };
    }

    // Phase 1 Task 1.2: 心跳定时器管理
    private void StartHeartbeatTimer()
    {
        // 先停止旧的定时器（如果存在）
        StopHeartbeatTimer();

        // 创建新的定时器，每30秒触发一次
        // 注意：使用 Task.Run 包装避免 async void 导致异常无法捕获
        _heartbeatTimer = new System.Threading.Timer(
            callback: _ => Task.Run(async () =>
            {
                try
                {
                    await UpdateSessionHeartbeatAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "心跳定时器回调执行失败");
                }
            }),
            state: null,
            dueTime: TimeSpan.FromSeconds(_heartbeatIntervalSeconds),
            period: TimeSpan.FromSeconds(_heartbeatIntervalSeconds)
        );

        Log.Debug("心跳定时器已启动，间隔 {Interval} 秒", _heartbeatIntervalSeconds);
    }

    private void StopHeartbeatTimer()
    {
        if (_heartbeatTimer != null)
        {
            _heartbeatTimer.Dispose();
            _heartbeatTimer = null;
            Log.Debug("心跳定时器已停止");
        }
    }

    private async Task UpdateSessionHeartbeatAsync()
    {
        var sessionId = _currentSessionId;
        if (sessionId == null)
            return;

        try
        {
            using var repository = _repositoryFactory();
            await repository.UpdateSessionHeartbeatAsync(sessionId.Value, DateTime.Now);
            Log.Debug("会话心跳已更新: SessionId={SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新会话心跳时发生错误: SessionId={SessionId}", sessionId);
        }
    }

    #region 空闲检查机制

    /// <summary>
    /// 启动空闲检查定时器
    /// </summary>
    private void StartIdleCheckTimer()
    {
        StopIdleCheckTimer();

        // 注意：使用 Task.Run 包装避免 async void 导致异常无法捕获
        _idleCheckTimer = new System.Threading.Timer(
            callback: _ => Task.Run(async () =>
            {
                try
                {
                    await PerformIdleCheckAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "空闲检查定时器回调执行失败");
                }
            }),
            state: null,
            dueTime: TimeSpan.FromSeconds(_idleCheckIntervalSeconds),
            period: TimeSpan.FromSeconds(_idleCheckIntervalSeconds)
        );

        Log.Debug("空闲检查定时器已启动，间隔 {Interval} 秒", _idleCheckIntervalSeconds);
    }

    /// <summary>
    /// 停止空闲检查定时器
    /// </summary>
    private void StopIdleCheckTimer()
    {
        if (_idleCheckTimer != null)
        {
            _idleCheckTimer.Dispose();
            _idleCheckTimer = null;
            Log.Debug("空闲检查定时器已停止");
        }
    }

    /// <summary>
    /// 执行空闲检查
    /// </summary>
    private async Task PerformIdleCheckAsync()
    {
        if (_currentSessionId == null || !_isRunning)
            return;

        try
        {
            var idleTime = _inputMonitor.GetIdleTimeSeconds();

            if (idleTime > _idleTimeoutSeconds)
            {
                // 检查是否应该豁免
                var shouldExempt = await ShouldExemptFromIdleCheckAsync();

                if (!shouldExempt)
                {
                    // 暂停会话
                    await PauseSessionDueToIdleAsync(idleTime);
                }
                else
                {
                    Log.Debug("空闲超时但豁免检查，继续计时: IdleTime={IdleTime}s", idleTime);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "执行空闲检查时发生错误");
        }
    }

    /// <summary>
    /// 判断当前会话是否应该豁免空闲检查
    /// </summary>
    private async Task<bool> ShouldExemptFromIdleCheckAsync()
    {
        if (_currentSessionId == null)
            return false;

        try
        {
            using var repository = _repositoryFactory();
            var session = await repository.GetSessionByIdAsync(_currentSessionId.Value);

            if (session == null)
                return false;

            // 重新收集系统状态（媒体播放状态可能已变化）
            var window = new WindowInfo
            {
                ProcessName = session.ProcessName,
                WindowTitle = "", // 不需要标题
                IsFullscreen = false
            };

            var systemState = CollectSystemState(window);

            // 根据活动类型和系统状态判断是否豁免
            switch (session.ActivityType)
            {
                case ActivityType.Video:
                    // Video 类型：必须仍在播放（有媒体会话或音频活动）
                    return systemState.MediaSessionPlaying || systemState.AudioActive;

                case ActivityType.Meeting:
                    // Meeting 类型：必须有音频活动（表示会议仍在进行）
                    return systemState.AudioActive;

                case ActivityType.PassiveBrowsing:
                    // 在线会议工具：必须有音频活动
                    if (_activityDetector.IsOnlineMeeting(session.ProcessName))
                    {
                        return systemState.AudioActive;
                    }
                    // 音乐播放器：必须有音频活动
                    if (_activityDetector.IsMusicPlayer(session.ProcessName))
                    {
                        return systemState.AudioActive;
                    }
                    return false;

                default:
                    return false; // 其他类型不豁免
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "判断豁免逻辑时发生错误");
            return false; // 出错时不豁免，安全起见
        }
    }

    /// <summary>
    /// 因空闲暂停会话
    /// </summary>
    private async Task PauseSessionDueToIdleAsync(int idleSeconds)
    {
        if (_currentSessionId == null)
            return;

        try
        {
            await _sessionLock.WaitAsync();
            try
            {
                // 结束时间 = 当前时间 - 空闲时长
                var endTime = DateTime.Now.AddSeconds(-idleSeconds);

                // 停止心跳定时器
                StopHeartbeatTimer();

                using var repository = _repositoryFactory();
                await repository.EndSessionAsync(_currentSessionId.Value, endTime);

                var session = await repository.GetSessionByIdAsync(_currentSessionId.Value);
                if (session != null)
                {
                    SessionEnded?.Invoke(this, session);
                }

                Log.Information("会话因空闲暂停: SessionId={SessionId}, IdleTime={IdleSeconds}s, EndTime={EndTime}",
                                _currentSessionId, idleSeconds, endTime);

                _currentSessionId = null;
                _sessionPausedDueToIdle = true;
            }
            finally
            {
                _sessionLock.Release();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "暂停会话时发生错误: SessionId={SessionId}", _currentSessionId);
        }
    }

    /// <summary>
    /// 用户活动检测事件处理（用于会话恢复）
    /// </summary>
    private void OnUserActivityDetected(object? sender, EventArgs e)
    {
        // 仅当会话因空闲而暂停时才响应
        if (_sessionPausedDueToIdle && _currentSessionId == null)
        {
            _ = CheckAndResumeSessionAsync();
        }
    }

    /// <summary>
    /// 检查并恢复会话
    /// </summary>
    private Task CheckAndResumeSessionAsync()
    {
        try
        {
            // 获取当前窗口信息（需要 WindowMonitor 提供此方法）
            // 注意：这里假设有获取当前窗口的方法，实际可能需要修改 WindowMonitor
            // 暂时通过窗口焦点变化来恢复，用户操作会自然触发

            _sessionPausedDueToIdle = false;
            Log.Debug("检测到用户活动，会话暂停标记已重置");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "检查并恢复会话时发生错误");
        }

        return Task.CompletedTask;
    }

    #endregion

    private bool _disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _isRunning = false;

            StopHeartbeatTimer();
            StopIdleCheckTimer();

            var lockAcquired = _sessionLock.Wait(TimeSpan.FromSeconds(5));

            try
            {
                _windowMonitor?.Stop();
                _inputMonitor?.Stop();
                _mediaDetector?.Stop();

                if (_windowMonitor != null)
                {
                    _windowMonitor.WindowFocusChanged -= OnWindowFocusChanged;
                }
                if (_inputMonitor != null)
                {
                    _inputMonitor.UserActivityDetected -= OnUserActivityDetected;
                }
                
                _sha256.Dispose();
            }
            finally
            {
                if (lockAcquired)
                {
                    _sessionLock.Release();
                }
                _sessionLock?.Dispose();
            }

            Log.Debug("SessionManager 已释放资源");
        }

        _disposed = true;
    }
}
