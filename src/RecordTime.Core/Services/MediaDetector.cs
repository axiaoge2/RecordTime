using System.Runtime.InteropServices;
using System.Diagnostics;
using Serilog;

namespace RecordTime.Core.Services;

/// <summary>
/// Windows媒体检测实现 (性能优化版)
/// 注意：完整的媒体检测需要Windows.Media API (UWP)
/// 这里提供基础实现，主要依赖音频检测和进程匹配
/// 性能优化: 使用进程缓存避免频繁调用 Process.GetProcesses()
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

    private readonly HashSet<string> _audioPlayerProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "spotify", "cloudmusic", "qqmusic", "wmplayer", "kugou", "kuwo",
        "foobar2000", "aimp", "musicbee"
    };

    private bool _isRunning;

    // 性能优化: 进程缓存机制
    private readonly object _cacheLock = new();
    private HashSet<string> _cachedVideoProcesses = new(StringComparer.OrdinalIgnoreCase);
    private HashSet<string> _cachedAudioProcesses = new(StringComparer.OrdinalIgnoreCase);
    private DateTime _lastCacheUpdate = DateTime.MinValue;
    private readonly TimeSpan _cacheRefreshInterval = TimeSpan.FromSeconds(5); // 5秒刷新一次缓存
    private System.Threading.Timer? _cacheRefreshTimer;

    public void Start()
    {
        _isRunning = true;

        // 立即刷新一次缓存
        RefreshProcessCache();

        // 启动定时器，每5秒刷新缓存
        _cacheRefreshTimer = new System.Threading.Timer(
            callback: _ => RefreshProcessCache(),
            state: null,
            dueTime: _cacheRefreshInterval,
            period: _cacheRefreshInterval
        );

        Log.Debug("MediaDetector 已启动，缓存刷新间隔: {Interval}秒", _cacheRefreshInterval.TotalSeconds);
    }

    public void Stop()
    {
        _isRunning = false;

        // 停止定时器
        _cacheRefreshTimer?.Dispose();
        _cacheRefreshTimer = null;

        // 清空缓存
        lock (_cacheLock)
        {
            _cachedVideoProcesses.Clear();
            _cachedAudioProcesses.Clear();
        }

        Log.Debug("MediaDetector 已停止");
    }

    /// <summary>
    /// 刷新进程缓存 (后台异步执行，避免阻塞)
    /// </summary>
    private void RefreshProcessCache()
    {
        try
        {
            var videoProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var audioProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 获取所有进程（这是昂贵的操作，但现在只在后台定时执行）
            var processes = Process.GetProcesses();

            foreach (var process in processes)
            {
                try
                {
                    var processName = process.ProcessName;

                    // 检查是否为视频播放器
                    if (_videoPlayerProcesses.Contains(processName))
                    {
                        videoProcesses.Add(processName);
                    }

                    // 检查是否为音频播放器
                    if (_audioPlayerProcesses.Contains(processName))
                    {
                        audioProcesses.Add(processName);
                    }
                }
                catch
                {
                    // 忽略无法访问的进程
                }
                finally
                {
                    process.Dispose();
                }
            }

            // 原子更新缓存
            lock (_cacheLock)
            {
                _cachedVideoProcesses = videoProcesses;
                _cachedAudioProcesses = audioProcesses;
                _lastCacheUpdate = DateTime.Now;
            }

            Log.Verbose("进程缓存已刷新: 视频={VideoCount}, 音频={AudioCount}",
                videoProcesses.Count, audioProcesses.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "刷新进程缓存时发生错误");
        }
    }

    public bool IsMediaPlaying()
    {
        if (!_isRunning)
            return false;

        try
        {
            // 性能优化: 直接使用缓存的进程列表，避免每次都调用 Process.GetProcesses()
            lock (_cacheLock)
            {
                // 如果缓存中有视频或音频进程，则认为正在播放
                return _cachedVideoProcesses.Count > 0 || _cachedAudioProcesses.Count > 0;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "检测媒体播放状态时发生错误");
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
        // 性能优化: 使用缓存的音频进程列表
        try
        {
            lock (_cacheLock)
            {
                return _cachedAudioProcesses.Count > 0;
            }
        }
        catch
        {
            return false;
        }
    }

    public MediaInfo? GetCurrentMediaInfo()
    {
        // 性能优化: 使用缓存的进程列表
        try
        {
            lock (_cacheLock)
            {
                // 优先返回视频播放器
                if (_cachedVideoProcesses.Count > 0)
                {
                    var firstVideo = _cachedVideoProcesses.First();
                    return new MediaInfo
                    {
                        SourceAppName = firstVideo,
                        IsPlaying = true,
                        Title = "正在播放"
                    };
                }

                // 其次返回音频播放器
                if (_cachedAudioProcesses.Count > 0)
                {
                    var firstAudio = _cachedAudioProcesses.First();
                    return new MediaInfo
                    {
                        SourceAppName = firstAudio,
                        IsPlaying = true,
                        Title = "正在播放"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取媒体信息时发生错误");
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
