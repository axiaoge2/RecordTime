using RecordTime.Core.Models;

namespace RecordTime.Core.Services;

/// <summary>
/// 活动类型检测器
/// </summary>
public class ActivityDetector : IActivityDetector
{
    // 已知的视频播放器进程名
    private static readonly HashSet<string> VideoAppProcesses = new(StringComparer.OrdinalIgnoreCase)
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
        "douyu", "huya", "kuaishou", "douyin"
    };

    // 已知的浏览器进程名
    private static readonly HashSet<string> BrowserProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "opera", "vivaldi",
        "chromium", "edge", "iexplore", "safari",
        "maxthon", "360se", "360chrome", "sogouexplorer",
        "qqbrowser", "liebao", "tor"
    };

    // 在线会议工具
    private static readonly HashSet<string> OnlineMeetingApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "zoom", "teams", "skype", "webex", "dingtalk", "lark",
        "feishu", "腾讯会议", "tencent_meeting", "voovmeeting",
        "googlemeet", "meet", "discord" // Discord 也可能用于会议
    };

    // 音乐播放器
    private static readonly HashSet<string> MusicPlayerApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "spotify", "cloudmusic", "qqmusic", "kugou", "kuwo",
        "foobar2000", "aimp", "musicbee", "netease_cloud_music",
        "applemusic", "itunes", "vlc" // VLC 也可以播放音乐
    };

    // 桌面进程（系统桌面）
    private static readonly HashSet<string> DesktopProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "progman", "workerw"
    };

    public ActivityType DetermineActivity(WindowInfo window, SystemState state)
    {
        // 优先级1: 空闲检测（优先判断，避免误判）
        if (state.SystemIdle)
        {
            return ActivityType.Idle;
        }

        // 优先级2: 视频检测（按照用户需求，视频优先级最高）
        if (state.MediaSessionPlaying)
        {
            return ActivityType.Video; // 系统媒体会话活跃，高可信度
        }

        if (state.IsVideoApp && state.AudioActive)
        {
            return ActivityType.Video; // 视频应用且有音频活动
        }

        if (state.IsBrowser && state.BrowserVideoPlaying)
        {
            return ActivityType.Video; // 浏览器视频播放
        }

        // 优先级3: 在线会议检测（会议应用 + 音频活动）
        string processLower = window.ProcessName.ToLowerInvariant();
        if (IsOnlineMeeting(window.ProcessName) && state.AudioActive)
        {
            return ActivityType.Meeting; // 会议应用且有音频活动，高可信度
        }

        // 优先级4: 游戏检测
        if (window.IsFullscreen && state.FrequentInput)
        {
            // 全屏且频繁输入，很可能是游戏
            return ActivityType.Gaming;
        }

        if ((processLower.Contains("game") || processLower.Contains("steam") ||
             processLower.Contains("epic") || processLower.Contains("origin")) &&
            state.FrequentInput)
        {
            return ActivityType.Gaming;
        }

        // 优先级5: 主动交互（提高阈值，更准确判断）
        if (state.KeyboardActivityLast30s > 20 ||
            (state.KeyboardActivityLast30s > 10 && state.MouseClicksLast30s > 5))
        {
            return ActivityType.ActiveTyping;
        }

        // 优先级6: 阅读模式检测（高滚轮活动 + 低键盘活动）
        // 典型场景：阅读文档、浏览文章、查看代码
        if (state.ScrollCountLast30s > 10 && state.KeyboardActivityLast30s < 5)
        {
            return ActivityType.Reading;
        }

        // 优先级7: 被动浏览
        if (state.WindowFocused)
        {
            return ActivityType.PassiveBrowsing;
        }

        // 默认：空闲
        return ActivityType.Idle;
    }

    public bool IsVideoPlaying(string processName)
    {
        return VideoAppProcesses.Contains(processName);
    }

    /// <summary>
    /// 检查是否为浏览器
    /// </summary>
    public bool IsBrowser(string processName)
    {
        return BrowserProcesses.Contains(processName);
    }

    /// <summary>
    /// 检查是否为在线会议工具
    /// </summary>
    public bool IsOnlineMeeting(string processName)
    {
        return OnlineMeetingApps.Contains(processName);
    }

    /// <summary>
    /// 检查是否为音乐播放器
    /// </summary>
    public bool IsMusicPlayer(string processName)
    {
        return MusicPlayerApps.Contains(processName);
    }

    /// <summary>
    /// 检查是否为桌面
    /// </summary>
    public bool IsDesktop(string processName)
    {
        return DesktopProcesses.Contains(processName);
    }

    // 缓存已解析的应用分类
    private static readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _categoryCache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// 根据进程名获取应用分类
    /// </summary>
    public string GetAppCategory(string processName)
    {
        if (string.IsNullOrEmpty(processName))
            return "其他应用";

        // 先查缓存
        if (_categoryCache.TryGetValue(processName, out var category))
        {
            return category;
        }

        var lowerProcessName = processName.ToLowerInvariant();
        string result;

        // 视频娱乐
        if (VideoAppProcesses.Contains(lowerProcessName))
            result = "视频娱乐";
        // 浏览器
        else if (BrowserProcesses.Contains(lowerProcessName))
            result = "浏览器";
        // 在线会议
        else if (OnlineMeetingApps.Contains(lowerProcessName))
            result = "在线会议";
        // 音乐
        else if (MusicPlayerApps.Contains(lowerProcessName))
            result = "音乐";
        // 桌面
        else if (DesktopProcesses.Contains(lowerProcessName))
            result = "系统桌面";
        // 开发工具
        else if (lowerProcessName.Contains("code") || lowerProcessName.Contains("studio") ||
            lowerProcessName.Contains("idea") || lowerProcessName.Contains("eclipse") ||
            lowerProcessName.Contains("rider") || lowerProcessName.Contains("pycharm") ||
            lowerProcessName.Contains("webstorm") || lowerProcessName.Contains("cursor") ||
            lowerProcessName.Contains("sublime") || lowerProcessName.Contains("vim") ||
            lowerProcessName.Contains("emacs") || lowerProcessName.Contains("atom"))
            result = "开发工具";
        // 办公软件
        else if (lowerProcessName.Contains("word") || lowerProcessName.Contains("excel") ||
            lowerProcessName.Contains("powerpoint") || lowerProcessName.Contains("notion") ||
            lowerProcessName.Contains("onenote") || lowerProcessName.Contains("outlook") ||
            lowerProcessName.Contains("winword") || lowerProcessName.Contains("wps") ||
            lowerProcessName.Contains("evernote") || lowerProcessName.Contains("obsidian"))
            result = "办公软件";
        // 社交通讯
        else if (lowerProcessName.Contains("wechat") || lowerProcessName.Contains("qq") ||
            lowerProcessName.Contains("telegram") || lowerProcessName.Contains("discord") ||
            lowerProcessName.Contains("slack") || lowerProcessName.Contains("teams") ||
            lowerProcessName.Contains("zoom") || lowerProcessName.Contains("dingtalk") ||
            lowerProcessName.Contains("feishu") || lowerProcessName.Contains("skype"))
            result = "社交通讯";
        // 游戏
        else if (lowerProcessName.Contains("game") || lowerProcessName.Contains("steam") ||
            lowerProcessName.Contains("origin") || lowerProcessName.Contains("epic") ||
            lowerProcessName.Contains("league") || lowerProcessName.Contains("dota") ||
            lowerProcessName.Contains("minecraft") || lowerProcessName.Contains("genshin") ||
            lowerProcessName.Contains("yuanshen") || lowerProcessName.Contains("wegame"))
            result = "游戏";
        // 音乐
        else if (lowerProcessName.Contains("spotify") || lowerProcessName.Contains("music") ||
            lowerProcessName.Contains("cloudmusic") || lowerProcessName.Contains("qqmusic") ||
            lowerProcessName.Contains("foobar") || lowerProcessName.Contains("aimp"))
            result = "音乐";
        // 图形设计
        else if (lowerProcessName.Contains("photoshop") || lowerProcessName.Contains("illustrator") ||
            lowerProcessName.Contains("figma") || lowerProcessName.Contains("sketch") ||
            lowerProcessName.Contains("blender") || lowerProcessName.Contains("gimp"))
            result = "图形设计";
        // 系统工具
        else if (lowerProcessName.Contains("terminal") || lowerProcessName.Contains("cmd") ||
            lowerProcessName.Contains("powershell") || lowerProcessName.Contains("explorer") ||
            lowerProcessName.Contains("taskmgr") || lowerProcessName.Contains("settings"))
            result = "系统工具";
        else
            result = "其他应用";

        // 写入缓存
        _categoryCache[processName] = result;
        return result;
    }
}
