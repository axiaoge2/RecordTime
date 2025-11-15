using System.Runtime.InteropServices;
using System.Text;
using System.Diagnostics;
using RecordTime.Core.Models;

namespace RecordTime.Core.Services;

/// <summary>
/// Windows窗口监控实现
/// </summary>
public class WindowMonitor : IWindowMonitor
{
    private Timer? _monitorTimer;
    private WindowInfo? _lastWindow;

    // 性能优化: 2秒检查一次足够,500ms 太频繁
    private const int MONITOR_INTERVAL_MS = 2000; // 2秒检查一次

    public event EventHandler<WindowInfo>? WindowFocusChanged;

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

        _monitorTimer = new Timer(MonitorCallback, null, 0, MONITOR_INTERVAL_MS);
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

    private void MonitorCallback(object? state)
    {
        try
        {
            var currentWindow = GetForegroundWindow();

            if (currentWindow == null)
            {
                Debug.WriteLine("⚠️ WindowMonitor: 当前窗口为空");
                return;
            }

            Debug.WriteLine($"🔍 WindowMonitor: 检测到窗口 - {currentWindow.ProcessName} | {currentWindow.WindowTitle}");

            // 检查窗口是否变化 - 只要进程名不同就触发
            bool isWindowChanged = _lastWindow == null ||
                _lastWindow.ProcessName != currentWindow.ProcessName;

            if (isWindowChanged)
            {
                Debug.WriteLine($"🔄 WindowMonitor: 窗口已变化,触发事件 - {currentWindow.ProcessName}");
                _lastWindow = currentWindow;
                WindowFocusChanged?.Invoke(this, currentWindow);
            }
            else
            {
                Debug.WriteLine($"⏸️ WindowMonitor: 窗口未变化,跳过 - {currentWindow.ProcessName}");
            }
        }
        catch (Exception ex)
        {
            // 日志记录
            Debug.WriteLine($"❌ WindowMonitor error: {ex.Message}");
        }
    }

    private WindowInfo? GetWindowInfoFromHandle(IntPtr hwnd)
    {
        if (!IsWindowVisible(hwnd))
        {
            Debug.WriteLine("⚠️ WindowInfo: 窗口不可见");
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
            var process = Process.GetProcessById((int)processId);
            processName = process.ProcessName;

            // 如果窗口标题为空但有进程名,也接受
            if (string.IsNullOrEmpty(windowTitle))
            {
                Debug.WriteLine($"⚠️ WindowInfo: 窗口标题为空,但进程名为 {processName}");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"❌ WindowInfo: 无法访问进程信息 - {ex.Message}");
            // 无法访问进程信息,可能是权限问题
            return null;
        }

        // 如果既没有标题也没有进程名,则忽略
        if (string.IsNullOrEmpty(windowTitle) && string.IsNullOrEmpty(processName))
        {
            Debug.WriteLine("⚠️ WindowInfo: 窗口标题和进程名都为空,忽略");
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
