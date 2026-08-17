using RecordTime.Core.Models;
using RecordTime.Core.Services;
using RecordTime.Core.Repositories;
using Xunit;

namespace RecordTime.Tests.Core;

/// <summary>
/// Minimal stub implementations for SessionManager unit tests.
/// These stubs avoid Win32 P/Invoke dependencies.
/// </summary>

public class StubWindowMonitor : IWindowMonitor
{
    public event EventHandler<WindowInfo>? WindowFocusChanged;
    public bool IsStarted { get; private set; }
    public WindowInfo? CurrentWindow { get; set; }

    public void Start() => IsStarted = true;
    public void Stop() => IsStarted = false;
    public WindowInfo? GetForegroundWindow() => CurrentWindow;
    public int GetSwitchCount(int minutes) => 0;
    public SwitchFrequencyStats GetSwitchStats() => new();

    public void SimulateFocusChange(WindowInfo window)
        => WindowFocusChanged?.Invoke(this, window);
}

public class StubInputMonitor : IInputMonitor
{
    public event EventHandler? UserActivityDetected;
    private int _idleTimeSeconds;

    public void Start() { }
    public void Stop() { }
    public int GetKeyboardActivityCount(int seconds) => 0;
    public int GetMouseClickCount(int seconds) => 0;
    public int GetMouseMovementDistance(int seconds) => 0;
    public int GetScrollCount(int seconds) => 0;
    public int GetIdleTimeSeconds() => _idleTimeSeconds;
    public InputActivityStats GetInputStats(int seconds) => new();

    public void SetIdleTime(int seconds) => _idleTimeSeconds = seconds;

    public void SimulateUserActivity()
        => UserActivityDetected?.Invoke(this, EventArgs.Empty);
}

public class StubMediaDetector : IMediaDetector
{
    public void Start() { }
    public void Stop() { }
    public bool IsMediaPlaying() => false;
    public bool IsProcessPlayingMedia(int processId) => false;
    public bool HasAudioActivity() => false;
    public MediaInfo? GetCurrentMediaInfo() => null;
    public bool IsVideoPlayer(string processName) => false;
    public bool IsBrowser(string processName) => false;
    public bool IsMusicPlayer(string processName) => false;
    public bool IsMusicPlayerRunning() => false;
    public bool IsVideoPlayerRunning() => false;
}

public class InMemorySessionRepository : ISessionRepository
{
    private readonly List<AppSession> _sessions = new();
    private int _nextId = 1;

    public Task<int> AddSessionAsync(AppSession session)
    {
        session.Id = _nextId++;
        _sessions.Add(session);
        return Task.FromResult(session.Id);
    }

    public Task UpdateSessionAsync(AppSession session) => Task.CompletedTask;

    public Task EndSessionAsync(int sessionId, DateTime endTime)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
        {
            session.EndTime = endTime;
            session.DurationSeconds = (int)(endTime - session.StartTime).TotalSeconds;
        }
        return Task.CompletedTask;
    }

    public Task UpdateSessionHeartbeatAsync(int sessionId, DateTime heartbeatTime)
    {
        var session = _sessions.FirstOrDefault(s => s.Id == sessionId);
        if (session != null)
            session.LastHeartbeat = heartbeatTime;
        return Task.CompletedTask;
    }

    public Task<AppSession?> GetSessionByIdAsync(int sessionId)
        => Task.FromResult(_sessions.FirstOrDefault(s => s.Id == sessionId));

    public Task<List<AppSession>> GetSessionsByDateAsync(DateTime date)
        => Task.FromResult(_sessions.Where(s => s.StartTime.Date == date.Date).ToList());

    public Task<List<AppSession>> GetSessionsByDateRangeAsync(DateTime startDate, DateTime endDate)
        => Task.FromResult(_sessions.Where(s => s.StartTime >= startDate && s.StartTime <= endDate).ToList());

    public Task<Dictionary<string, int>> GetAppUsageStatsAsync(DateTime startDate, DateTime endDate)
        => Task.FromResult(new Dictionary<string, int>());

    public Task<Dictionary<ActivityType, int>> GetActivityStatsAsync(DateTime startDate, DateTime endDate)
        => Task.FromResult(new Dictionary<ActivityType, int>());

    public Task<AppSession?> GetActiveSessionAsync()
        => Task.FromResult(_sessions.FirstOrDefault(s => s.EndTime == null));

    public List<AppSession> GetAllSessions() => _sessions.ToList();

    public void Dispose() { }
}

public class SessionManagerTests
{
    private readonly StubWindowMonitor _windowMonitor = new();
    private readonly StubInputMonitor _inputMonitor = new();
    private readonly StubMediaDetector _mediaDetector = new();
    private readonly ActivityDetector _activityDetector = new();
    private readonly InMemorySessionRepository _repository = new();

    private SessionManager CreateManager(int idleTimeoutSeconds = 300, int idleCheckIntervalSeconds = 120)
    {
        return new SessionManager(
            _windowMonitor,
            _inputMonitor,
            _mediaDetector,
            _activityDetector,
            () => _repository,
            idleTimeoutSeconds,
            idleCheckIntervalSeconds);
    }

    private async Task WaitUntilAsync(Func<bool> condition, int timeoutMs = 10000)
    {
        var deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
        while (DateTime.UtcNow < deadline)
        {
            if (condition())
                return;
            await Task.Delay(50);
        }
    }

    private static WindowInfo MakeWindow(string processName = "notepad.exe") => new()
    {
        ProcessName = processName,
        WindowTitle = "Test Window",
        IsFullscreen = false,
        IsFocused = true
    };

    [Fact]
    public void Start_SubscribesEventsAndStartsMonitors()
    {
        var manager = CreateManager();

        manager.Start();

        Assert.True(_windowMonitor.IsStarted);
    }

    [Fact]
    public async Task StopAsync_UnsubscribesEventsAndStopsMonitors()
    {
        var manager = CreateManager();
        manager.Start();

        await manager.StopAsync();

        Assert.False(_windowMonitor.IsStarted);
    }

    [Fact]
    public async Task Start_DoubleStart_IsIdempotent()
    {
        var manager = CreateManager();
        manager.Start();
        manager.Start(); // should not throw

        await manager.StopAsync();
    }

    [Fact]
    public async Task StopAsync_WithoutStart_IsIdempotent()
    {
        var manager = CreateManager();
        await manager.StopAsync(); // should not throw
    }

    [Fact]
    public async Task WindowFocusChanged_CreatesNewSession()
    {
        var manager = CreateManager();
        manager.Start();

        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));

        // Allow async event handler to complete
        await Task.Delay(100);

        var sessions = _repository.GetAllSessions();
        Assert.Single(sessions);
        Assert.Equal("notepad.exe", sessions[0].ProcessName);
        Assert.Null(sessions[0].EndTime);

        await manager.StopAsync();
    }

    [Fact]
    public async Task WindowFocusChanged_SecondWindow_EndsPreviousSession()
    {
        var manager = CreateManager();
        manager.Start();

        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));
        await Task.Delay(100);

        _windowMonitor.SimulateFocusChange(MakeWindow("chrome.exe"));
        await Task.Delay(100);

        var sessions = _repository.GetAllSessions();
        Assert.Equal(2, sessions.Count);

        // First session should be ended
        Assert.NotNull(sessions[0].EndTime);
        // Second session should be active
        Assert.Null(sessions[1].EndTime);

        await manager.StopAsync();
    }

    [Fact]
    public async Task UserActivityAfterIdlePause_RestartsSession()
    {
        var manager = CreateManager(idleTimeoutSeconds: 5, idleCheckIntervalSeconds: 1);
        manager.Start();

        _windowMonitor.CurrentWindow = MakeWindow("notepad.exe");
        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));
        await Task.Delay(100);

        Assert.Single(_repository.GetAllSessions());

        // 超过空闲阈值，等待空闲检查暂停会话
        _inputMonitor.SetIdleTime(30);
        await WaitUntilAsync(() => _repository.GetAllSessions().FirstOrDefault()?.EndTime != null);

        var sessions = _repository.GetAllSessions();
        Assert.Single(sessions);
        Assert.NotNull(sessions[0].EndTime);

        // 同一窗口内恢复活动：应重新开始记录，无需切换前台窗口
        _inputMonitor.SetIdleTime(0);
        _inputMonitor.SimulateUserActivity();
        await WaitUntilAsync(() => _repository.GetAllSessions().Count == 2);

        sessions = _repository.GetAllSessions();
        Assert.Null(sessions[1].EndTime);
        Assert.Equal("notepad.exe", sessions[1].ProcessName);

        await manager.StopAsync();
    }

    [Fact]
    public async Task StopAsync_EndsActiveSession()
    {
        var manager = CreateManager();
        manager.Start();

        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));
        await Task.Delay(100);

        await manager.StopAsync();

        var sessions = _repository.GetAllSessions();
        Assert.Single(sessions);
        Assert.NotNull(sessions[0].EndTime);
    }

    [Fact]
    public async Task SessionStarted_EventFires()
    {
        var manager = CreateManager();
        AppSession? startedSession = null;
        manager.SessionStarted += (s, session) => startedSession = session;

        manager.Start();
        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));
        await Task.Delay(100);

        Assert.NotNull(startedSession);
        Assert.Equal("notepad.exe", startedSession!.ProcessName);

        await manager.StopAsync();
    }

    [Fact]
    public async Task SessionEnded_EventFires()
    {
        var manager = CreateManager();
        AppSession? endedSession = null;
        manager.SessionEnded += (s, session) => endedSession = session;

        manager.Start();
        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));
        await Task.Delay(100);

        _windowMonitor.SimulateFocusChange(MakeWindow("chrome.exe"));
        await Task.Delay(100);

        Assert.NotNull(endedSession);
        Assert.Equal("notepad.exe", endedSession!.ProcessName);

        await manager.StopAsync();
    }

    [Fact]
    public async Task Dispose_CleansUpResources()
    {
        var manager = CreateManager();
        manager.Start();

        _windowMonitor.SimulateFocusChange(MakeWindow("notepad.exe"));
        await Task.Delay(100);

        manager.Dispose();

        // Should not throw on double dispose
        manager.Dispose();
    }

    [Fact]
    public async Task WindowTitleHashed_NotStoredInPlaintext()
    {
        var manager = CreateManager();
        manager.Start();

        var window = MakeWindow("notepad.exe");
        window.WindowTitle = "Secret Document.txt";
        _windowMonitor.SimulateFocusChange(window);
        await Task.Delay(100);

        var sessions = _repository.GetAllSessions();
        Assert.Single(sessions);
        // Title should be hashed, not the original
        Assert.NotEqual("Secret Document.txt", sessions[0].WindowTitleHash);

        await manager.StopAsync();
    }
}
