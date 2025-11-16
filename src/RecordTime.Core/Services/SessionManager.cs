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
    private bool _isRunning;

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

        // 取消订阅
        _windowMonitor.WindowFocusChanged -= OnWindowFocusChanged;

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
        var keyboardActivity = _inputMonitor.GetKeyboardActivityCount(30);
        var mouseClicks = _inputMonitor.GetMouseClickCount(30);
        var mouseMovement = _inputMonitor.GetMouseMovementDistance(30);
        var idleTime = _inputMonitor.GetIdleTimeSeconds();

        var isMediaPlaying = _mediaDetector.IsMediaPlaying();
        var isVideoPlayer = _mediaDetector.IsVideoPlayer(window.ProcessName);
        var isBrowser = _mediaDetector.IsBrowser(window.ProcessName);
        var hasAudio = _mediaDetector.HasAudioActivity();

        return new SystemState
        {
            MediaSessionPlaying = isMediaPlaying,
            IsVideoApp = isVideoPlayer,
            AudioActive = hasAudio,
            IsBrowser = isBrowser,
            BrowserVideoPlaying = isBrowser && isMediaPlaying,
            WindowFocused = true,
            SystemIdle = idleTime > _idleTimeoutSeconds,
            KeyboardActivityLast30s = keyboardActivity,
            MouseClicksLast30s = mouseClicks,
            FrequentInput = keyboardActivity > 20 || mouseClicks > 10,
            HighGpuUsage = false // 简化实现，实际需要检测GPU使用率
        };
    }

    private async Task StartNewSessionAsync(WindowInfo window, ActivityType activityType, string category)
    {
        using var repository = _repositoryFactory();

        // 获取友好的显示名称
        var friendlyName = AppNameResolver.GetFriendlyName(window.ProcessName);

        var session = new AppSession
        {
            ProcessName = window.ProcessName,
            DisplayName = friendlyName,
            ActivityType = activityType,
            Category = category,
            StartTime = DateTime.Now,
            Confidence = CalculateConfidence(activityType),
            WindowTitleHash = HashWindowTitle(window.WindowTitle)
        };

        _currentSessionId = await repository.AddSessionAsync(session);
        session.Id = _currentSessionId.Value;

        SessionStarted?.Invoke(this, session);

        Log.Information("会话已开始: {DisplayName} [{ProcessName}] ({ActivityType})", friendlyName, window.ProcessName, activityType);
    }

    private string HashWindowTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(title);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private async Task EndCurrentSessionAsync()
    {
        if (_currentSessionId == null)
            return;

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
            ActivityType.ActiveTyping => 80,
            ActivityType.PassiveBrowsing => 60,
            ActivityType.Idle => 100,
            _ => 50
        };
    }

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
            // 同步清理
            _isRunning = false;

            // 尝试获取锁,但设置超时避免死锁
            var lockAcquired = _sessionLock.Wait(TimeSpan.FromSeconds(5));

            try
            {
                // 同步停止所有监控服务
                _windowMonitor?.Stop();
                _inputMonitor?.Stop();
                _mediaDetector?.Stop();

                // 取消订阅事件
                if (_windowMonitor != null)
                {
                    _windowMonitor.WindowFocusChanged -= OnWindowFocusChanged;
                }
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
