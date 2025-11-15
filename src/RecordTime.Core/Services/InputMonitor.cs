using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RecordTime.Core.Services;

/// <summary>
/// Windows输入监控实现
/// </summary>
public class InputMonitor : IInputMonitor
{
    private readonly object _lock = new();
    private readonly Queue<DateTime> _keyboardEvents = new();
    private readonly Queue<DateTime> _mouseClickEvents = new();
    private readonly Queue<(DateTime Time, int Distance)> _mouseMovements = new();

    // 性能优化: 使用计数器避免频繁遍历
    private int _keyboardCount = 0;
    private int _mouseClickCount = 0;
    private const int CLEANUP_THRESHOLD = 1000; // 达到1000条记录时触发清理

    private IntPtr _keyboardHookId = IntPtr.Zero;
    private IntPtr _mouseHookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _keyboardProc;
    private LowLevelMouseProc? _mouseProc;
    private System.Drawing.Point _lastMousePos;

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
    private const int WM_MOUSEMOVE = 0x0200;

    #region Win32 API

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelMouseProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string lpModuleName);

    [DllImport("user32.dll")]
    private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

    [DllImport("kernel32.dll")]
    private static extern uint GetTickCount();

    [StructLayout(LayoutKind.Sequential)]
    private struct LASTINPUTINFO
    {
        public uint cbSize;
        public uint dwTime;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    private delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);

    #endregion

    public void Start()
    {
        if (_keyboardHookId != IntPtr.Zero)
            return;

        // 创建钩子委托（必须保持引用，否则会被GC回收）
        _keyboardProc = KeyboardHookCallback;
        _mouseProc = MouseHookCallback;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;

        if (curModule != null)
        {
            _keyboardHookId = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardProc,
                GetModuleHandle(curModule.ModuleName), 0);

            _mouseHookId = SetWindowsHookEx(WH_MOUSE_LL, _mouseProc,
                GetModuleHandle(curModule.ModuleName), 0);
        }

        Debug.WriteLine("✅ InputMonitor started");
    }

    public void Stop()
    {
        if (_keyboardHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookId);
            _keyboardHookId = IntPtr.Zero;
        }

        if (_mouseHookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_mouseHookId);
            _mouseHookId = IntPtr.Zero;
        }

        Debug.WriteLine("⏹️ InputMonitor stopped");
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
        {
            var now = DateTime.Now;
            _keyboardEvents.Enqueue(now);
            System.Threading.Interlocked.Increment(ref _keyboardCount);

            // 异步清理过期数据,避免在Hook回调中执行耗时操作
            if (_keyboardCount > CLEANUP_THRESHOLD)
            {
                _ = Task.Run(() => CleanupKeyboardEvents());
            }
        }

        return CallNextHookEx(_keyboardHookId, nCode, wParam, lParam);
    }

    private void CleanupKeyboardEvents()
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now.AddMinutes(-5);
            while (_keyboardEvents.Count > 0 && _keyboardEvents.Peek() < cutoff)
            {
                _keyboardEvents.Dequeue();
                System.Threading.Interlocked.Decrement(ref _keyboardCount);
            }
        }
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var now = DateTime.Now;

            // 鼠标点击
            if (wParam == (IntPtr)WM_LBUTTONDOWN ||
                wParam == (IntPtr)WM_RBUTTONDOWN ||
                wParam == (IntPtr)WM_MBUTTONDOWN)
            {
                _mouseClickEvents.Enqueue(now);
                System.Threading.Interlocked.Increment(ref _mouseClickCount);

                // 异步清理
                if (_mouseClickCount > CLEANUP_THRESHOLD)
                {
                    _ = Task.Run(() => CleanupMouseClickEvents());
                }
            }

            // 鼠标移动 - 简化处理,减少Marshal操作
            if (wParam == (IntPtr)WM_MOUSEMOVE)
            {
                var hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var currentPos = new System.Drawing.Point(hookStruct.pt.X, hookStruct.pt.Y);

                if (_lastMousePos != System.Drawing.Point.Empty)
                {
                    var distance = Math.Abs(currentPos.X - _lastMousePos.X) +
                                 Math.Abs(currentPos.Y - _lastMousePos.Y);

                    if (distance > 5) // 忽略微小移动
                    {
                        _mouseMovements.Enqueue((now, distance));

                        // 异步清理鼠标移动数据
                        if (_mouseMovements.Count > CLEANUP_THRESHOLD)
                        {
                            _ = Task.Run(() => CleanupMouseMovements());
                        }
                    }
                }

                _lastMousePos = currentPos;
            }
        }

        return CallNextHookEx(_mouseHookId, nCode, wParam, lParam);
    }

    private void CleanupMouseClickEvents()
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now.AddMinutes(-5);
            while (_mouseClickEvents.Count > 0 && _mouseClickEvents.Peek() < cutoff)
            {
                _mouseClickEvents.Dequeue();
                System.Threading.Interlocked.Decrement(ref _mouseClickCount);
            }
        }
    }

    private void CleanupMouseMovements()
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now.AddMinutes(-5);
            while (_mouseMovements.Count > 0 && _mouseMovements.Peek().Time < cutoff)
            {
                _mouseMovements.Dequeue();
            }
        }
    }

    public int GetKeyboardActivityCount(int seconds)
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now.AddSeconds(-seconds);
            return _keyboardEvents.Count(e => e >= cutoff);
        }
    }

    public int GetMouseClickCount(int seconds)
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now.AddSeconds(-seconds);
            return _mouseClickEvents.Count(e => e >= cutoff);
        }
    }

    public int GetMouseMovementDistance(int seconds)
    {
        lock (_lock)
        {
            var cutoff = DateTime.Now.AddSeconds(-seconds);
            return _mouseMovements
                .Where(m => m.Time >= cutoff)
                .Sum(m => m.Distance);
        }
    }

    public int GetIdleTimeSeconds()
    {
        var lastInputInfo = new LASTINPUTINFO();
        lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);

        if (GetLastInputInfo(ref lastInputInfo))
        {
            var idleTime = GetTickCount() - lastInputInfo.dwTime;
            return (int)(idleTime / 1000);
        }

        return 0;
    }

    ~InputMonitor()
    {
        Stop();
    }
}
