using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using System.Threading;
using RecordTime.Core.Models;
using Serilog;

namespace RecordTime.Core.Services;

/// <summary>
/// Windows窗口监控实现 (性能优化版)
/// 性能优化: 根据用户活跃度动态调整轮询间隔，活跃时快速响应，空闲时降低CPU占用
/// </summary>
public class WindowMonitor : IWindowMonitor
{
    private Timer? _monitorTimer;
    private WindowInfo? _lastWindow;
    private readonly int _monitorIntervalMs;

    // 性能优化: 动态轮询间隔
    private int _currentIntervalMs;
    private const int ACTIVE_INTERVAL_MS = 500;    // 活跃时：500ms
    private const int IDLE_INTERVAL_MS = 2000;     // 空闲时：2000ms
    private DateTime _lastWindowChange = DateTime.Now;
    private const int IDLE_THRESHOLD_SECONDS = 30;  // 30秒无窗口切换视为空闲

    // 应用切换频率追踪
    private readonly Queue<DateTime> _switchEvents = new();
    private readonly object _switchLock = new();
    private const int SWITCH_EVENT_RETENTION_MINUTES = 10; // 保留10分钟的切换事件

    public event EventHandler<WindowInfo>? WindowFocusChanged;

    public WindowMonitor() : this(ACTIVE_INTERVAL_MS) // 默认使用活跃间隔
    {
    }

    public WindowMonitor(int monitorIntervalMs)
    {
        _monitorIntervalMs = monitorIntervalMs;
        _currentIntervalMs = monitorIntervalMs;
    }

    #region Win32 API

    [DllImport("user32.dll", EntryPoint = "GetForegroundWindow")]
    private static extern IntPtr GetForegroundWindow_Native();

    [DllImport("user32.dll")]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern int GetWindowTextLength(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    #endregion

    public void Start()
    {
        if (_monitorTimer != null)
            return;

        _monitorTimer = new Timer(MonitorCallback, null, 0, _monitorIntervalMs);
        Log.Debug("WindowMonitor 已启动，轮询间隔: {IntervalMs}ms", _monitorIntervalMs);
    }

    public void Stop()
    {
        _monitorTimer?.Dispose();
        _monitorTimer = null;
    }

    public WindowInfo? GetForegroundWindow()
    {
        var hwnd = GetForegroundWindow_Native();
        if (hwnd == IntPtr.Zero)
            return null;

        return GetWindowInfoFromHandle(hwnd);
    }

    private int _isCallbackRunning = 0;

    private void MonitorCallback(object? state)
    {
        if (Interlocked.CompareExchange(ref _isCallbackRunning, 1, 0) != 0)
            return;
        try
        {
            var currentWindow = GetForegroundWindow();

            if (currentWindow == null)
            {
                Log.Debug("WindowMonitor: 当前窗口为空");
                return;
            }

            Log.Verbose("检测到窗口: {ProcessName} | {WindowTitle}", currentWindow.ProcessName, currentWindow.WindowTitle);

            // 检查窗口是否变化 - 只要进程名不同就触发
            bool isWindowChanged = _lastWindow == null ||
                _lastWindow.ProcessName != currentWindow.ProcessName;

            if (isWindowChanged)
            {
                Log.Debug("窗口已变化,触发事件: {ProcessName}", currentWindow.ProcessName);
                _lastWindow = currentWindow;
                _lastWindowChange = DateTime.Now;

                // 记录切换事件
                RecordSwitchEvent();

                // 窗口切换后切换到活跃轮询模式
                AdjustPollingInterval(true);

                WindowFocusChanged?.Invoke(this, currentWindow);
            }
            else
            {
                Log.Verbose("窗口未变化,跳过: {ProcessName}", currentWindow.ProcessName);

                // 检查是否应该切换到空闲轮询模式
                var idleTime = (DateTime.Now - _lastWindowChange).TotalSeconds;
                if (idleTime > IDLE_THRESHOLD_SECONDS)
                {
                    AdjustPollingInterval(false);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "WindowMonitor 监控过程中发生错误");
        }
        finally
        {
            Interlocked.Exchange(ref _isCallbackRunning, 0);
        }
    }

    /// <summary>
    /// 动态调整轮询间隔 (性能优化)
    /// </summary>
    /// <param name="isActive">是否处于活跃状态</param>
    private void AdjustPollingInterval(bool isActive)
    {
        var targetInterval = isActive ? ACTIVE_INTERVAL_MS : IDLE_INTERVAL_MS;

        // 如果间隔已经匹配，跳过调整
        if (_currentIntervalMs == targetInterval)
            return;

        _currentIntervalMs = targetInterval;

        // 重新创建定时器以应用新的间隔
        _monitorTimer?.Dispose();
        _monitorTimer = new Timer(MonitorCallback, null, 0, _currentIntervalMs);

        Log.Debug("轮询间隔已调整: {Interval}ms ({Mode})",
            _currentIntervalMs, isActive ? "活跃模式" : "空闲模式");
    }

    /// <summary>
    /// 记录切换事件
    /// </summary>
    private void RecordSwitchEvent()
    {
        lock (_switchLock)
        {
            _switchEvents.Enqueue(DateTime.Now);
            CleanupOldSwitchEvents();
        }
    }

    /// <summary>
    /// 清理过期的切换事件
    /// </summary>
    private void CleanupOldSwitchEvents()
    {
        var cutoff = DateTime.Now.AddMinutes(-SWITCH_EVENT_RETENTION_MINUTES);
        while (_switchEvents.Count > 0 && _switchEvents.Peek() < cutoff)
        {
            _switchEvents.Dequeue();
        }
    }

    /// <summary>
    /// 获取指定时间范围内的应用切换次数
    /// </summary>
    /// <param name="minutes">时间范围（分钟）</param>
    /// <returns>切换次数</returns>
    public int GetSwitchCount(int minutes)
    {
        lock (_switchLock)
        {
            CleanupOldSwitchEvents();
            var cutoff = DateTime.Now.AddMinutes(-minutes);
            return _switchEvents.Count(e => e >= cutoff);
        }
    }

    /// <summary>
    /// 获取切换频率统计
    /// </summary>
    /// <returns>包含各时间窗口切换次数的统计</returns>
    public SwitchFrequencyStats GetSwitchStats()
    {
        lock (_switchLock)
        {
            CleanupOldSwitchEvents();
            var now = DateTime.Now;
            var cutoff1 = now.AddMinutes(-1);
            var cutoff5 = now.AddMinutes(-5);
            int count1 = 0, count5 = 0;

            foreach (var e in _switchEvents)
            {
                if (e >= cutoff5) count5++;
                if (e >= cutoff1) count1++;
            }

            return new SwitchFrequencyStats
            {
                SwitchesLast1Min = count1,
                SwitchesLast5Min = count5,
                SwitchesLast10Min = _switchEvents.Count
            };
        }
    }

    private WindowInfo? GetWindowInfoFromHandle(IntPtr hwnd)
    {
        if (!IsWindowVisible(hwnd))
        {
            Log.Verbose("窗口不可见,跳过");
            return null;
        }

        // 获取窗口标题
        var length = GetWindowTextLength(hwnd);
        var windowTitle = string.Empty;

        if (length > 0)
        {
            var sb = new StringBuilder(length + 1);
            GetWindowText(hwnd, sb, sb.Capacity);
            windowTitle = sb.ToString();
        }

        // 获取进程信息
        GetWindowThreadProcessId(hwnd, out uint processId);

        string processName = string.Empty;
        try
        {
            using var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;

            if (string.IsNullOrEmpty(windowTitle))
            {
                Log.Verbose("窗口标题为空,但进程名为: {ProcessName}", processName);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "无法访问进程信息,可能是权限问题: ProcessId={ProcessId}", processId);
            // 无法访问进程信息,可能是权限问题
            return null;
        }

        // 如果既没有标题也没有进程名,则忽略
        if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(processName))
        {
            Log.Verbose("窗口标题和进程名都为空,忽略");
            return null;
        }

        // 检查是否全屏
        var isFullscreen = IsFullScreen(hwnd);

        return new WindowInfo
        {
            Handle = hwnd,
            ProcessName = processName,
            WindowTitle = windowTitle,
            ProcessId = (int)processId,
            IsFullscreen = isFullscreen,
            IsFocused = true
        };
    }

    private bool IsFullScreen(IntPtr hwnd)
    {
        if (!GetWindowRect(hwnd, out RECT rect))
            return false;

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;

        // 简单判断：如果窗口大小接近屏幕分辨率
        var screen = System.Windows.Forms.Screen.PrimaryScreen?.Bounds;
        if (screen == null)
            return false;

        return Math.Abs(width - screen.Value.Width) < 10 &&
               Math.Abs(height - screen.Value.Height) < 10;
    }
}
