using System.Runtime.InteropServices;
using System.Diagnostics;

namespace RecordTime.Core.Services;

/// <summary>
/// Windows媒体检测实现
/// 注意：完整的媒体检测需要Windows.Media API (UWP)
/// 这里提供基础实现，主要依赖音频检测和进程匹配
/// </summary>
public class MediaDetector : IMediaDetector
{
    private readonly HashSet<string> _videoPlayerProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        // 桌面视频播放器
        "vlc", "potplayer", "potplayermini", "potplayer64",
        "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64",
        "wmplayer", "k-litecodecpackx64", "kmplayer",
        "gomplayer", "daum", "smplayer", "kodi",

        // 流媒体应用
        "netflix", "primevideo", "disney", "disneyplus",
        "hulu", "hbomax", "appletv",

        // 中文视频平台
        "bilibili", "bilibiliuwp", "tencent_video", "qqlivetv",
        "iqiyi", "iqiyiuwp", "youku", "manggotv",
        "douyu", "huya", "kuaishou", "douyin",

        // 其他
        "zoom", "teams", "skype", // 会议软件
        "obs", "obs64", "streamlabs" // 录屏/直播软件
    };

    private readonly HashSet<string> _browserProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "opera", "vivaldi",
        "chromium", "edge", "iexplore", "safari",
        "maxthon", "360se", "360chrome", "sogouexplorer",
        "qqbrowser", "liebao", "tor"
    };

    private bool _isRunning;

    public void Start()
    {
        _isRunning = true;
        Debug.WriteLine("✅ MediaDetector started");
    }

    public void Stop()
    {
        _isRunning = false;
        Debug.WriteLine("⏹️ MediaDetector stopped");
    }

    public bool IsMediaPlaying()
    {
        if (!_isRunning)
            return false;

        try
        {
            // 方法1: 检查已知视频播放器进程
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                try
                {
                    if (_videoPlayerProcesses.Contains(process.ProcessName))
                    {
                        // 检查进程是否有窗口且可见
                        if (IsProcessWindowVisible(process))
                        {
                            return true;
                        }
                    }
                }
                catch
                {
                    // 忽略无权访问的进程
                }
            }

            // 方法2: 检查是否有音频播放（简化版）
            // 实际生产环境应使用 Core Audio API
            return HasAudioActivity();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"MediaDetector error: {ex.Message}");
            return false;
        }
    }

    public bool IsProcessPlayingMedia(int processId)
    {
        try
        {
            var process = Process.GetProcessById(processId);
            return _videoPlayerProcesses.Contains(process.ProcessName) ||
                   _browserProcesses.Contains(process.ProcessName);
        }
        catch
        {
            return false;
        }
    }

    public bool HasAudioActivity()
    {
        // 简化实现：检查是否有音频相关进程活跃
        // 完整实现需要使用 Core Audio API (IAudioSessionManager2)
        try
        {
            var audioProcesses = new[] { "spotify", "cloudmusic", "qqmusic", "wmplayer" };
            var processes = Process.GetProcesses();

            return processes.Any(p =>
            {
                try
                {
                    return audioProcesses.Any(ap => p.ProcessName.Contains(ap, StringComparison.OrdinalIgnoreCase));
                }
                catch
                {
                    return false;
                }
            });
        }
        catch
        {
            return false;
        }
    }

    public MediaInfo? GetCurrentMediaInfo()
    {
        // 基础实现：返回检测到的播放器信息
        // 完整实现需要使用 Windows.Media.Control API
        try
        {
            var processes = Process.GetProcesses();
            foreach (var process in processes)
            {
                try
                {
                    if (_videoPlayerProcesses.Contains(process.ProcessName) &&
                        IsProcessWindowVisible(process))
                    {
                        return new MediaInfo
                        {
                            SourceAppName = process.ProcessName,
                            IsPlaying = true,
                            Title = "正在播放" // 实际需要从窗口标题或API获取
                        };
                    }
                }
                catch
                {
                    // 忽略
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"GetCurrentMediaInfo error: {ex.Message}");
        }

        return null;
    }

    private bool IsProcessWindowVisible(Process process)
    {
        try
        {
            var hwnd = process.MainWindowHandle;
            if (hwnd == IntPtr.Zero)
                return false;

            return IsWindowVisible(hwnd);
        }
        catch
        {
            return false;
        }
    }

    #region Win32 API

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    #endregion

    /// <summary>
    /// 检查进程是否为已知的视频播放器
    /// </summary>
    public bool IsVideoPlayer(string processName)
    {
        return _videoPlayerProcesses.Contains(processName);
    }

    /// <summary>
    /// 检查进程是否为浏览器
    /// </summary>
    public bool IsBrowser(string processName)
    {
        return _browserProcesses.Contains(processName);
    }
}
