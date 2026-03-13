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

        if (IsOnlineMeeting(window.ProcessName) && state.AudioActive)
        {
            return ActivityType.Meeting;
        }

        if (window.IsFullscreen && state.FrequentInput)
        {
            return ActivityType.Gaming;
        }

        if ((window.ProcessName.Contains("game", StringComparison.OrdinalIgnoreCase) ||
             window.ProcessName.Contains("steam", StringComparison.OrdinalIgnoreCase) ||
             window.ProcessName.Contains("epic", StringComparison.OrdinalIgnoreCase) ||
             window.ProcessName.Contains("origin", StringComparison.OrdinalIgnoreCase)) &&
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

        string result;
        var ci = StringComparison.OrdinalIgnoreCase;

        if (VideoAppProcesses.Contains(processName))
            result = "视频娱乐";
        else if (BrowserProcesses.Contains(processName))
            result = "浏览器";
        else if (OnlineMeetingApps.Contains(processName))
            result = "在线会议";
        else if (MusicPlayerApps.Contains(processName))
            result = "音乐";
        else if (DesktopProcesses.Contains(processName))
            result = "系统桌面";
        else if (processName.Contains("code", ci) || processName.Contains("studio", ci) ||
            processName.Contains("idea", ci) || processName.Contains("eclipse", ci) ||
            processName.Contains("rider", ci) || processName.Contains("pycharm", ci) ||
            processName.Contains("webstorm", ci) || processName.Contains("cursor", ci) ||
            processName.Contains("sublime", ci) || processName.Contains("vim", ci) ||
            processName.Contains("emacs", ci) || processName.Contains("atom", ci))
            result = "开发工具";
        else if (processName.Contains("word", ci) || processName.Contains("excel", ci) ||
            processName.Contains("powerpoint", ci) || processName.Contains("notion", ci) ||
            processName.Contains("onenote", ci) || processName.Contains("outlook", ci) ||
            processName.Contains("winword", ci) || processName.Contains("wps", ci) ||
            processName.Contains("evernote", ci) || processName.Contains("obsidian", ci))
            result = "办公软件";
        else if (processName.Contains("wechat", ci) || processName.Contains("qq", ci) ||
            processName.Contains("telegram", ci) || processName.Contains("discord", ci) ||
            processName.Contains("slack", ci) || processName.Contains("teams", ci) ||
            processName.Contains("zoom", ci) || processName.Contains("dingtalk", ci) ||
            processName.Contains("feishu", ci) || processName.Contains("skype", ci))
            result = "社交通讯";
        else if (processName.Contains("game", ci) || processName.Contains("steam", ci) ||
            processName.Contains("origin", ci) || processName.Contains("epic", ci) ||
            processName.Contains("league", ci) || processName.Contains("dota", ci) ||
            processName.Contains("minecraft", ci) || processName.Contains("genshin", ci) ||
            processName.Contains("yuanshen", ci) || processName.Contains("wegame", ci))
            result = "游戏";
        else if (processName.Contains("spotify", ci) || processName.Contains("music", ci) ||
            processName.Contains("cloudmusic", ci) || processName.Contains("qqmusic", ci) ||
            processName.Contains("foobar", ci) || processName.Contains("aimp", ci))
            result = "音乐";
        else if (processName.Contains("photoshop", ci) || processName.Contains("illustrator", ci) ||
            processName.Contains("figma", ci) || processName.Contains("sketch", ci) ||
            processName.Contains("blender", ci) || processName.Contains("gimp", ci))
            result = "图形设计";
        else if (processName.Contains("terminal", ci) || processName.Contains("cmd", ci) ||
            processName.Contains("powershell", ci) || processName.Contains("explorer", ci) ||
            processName.Contains("taskmgr", ci) || processName.Contains("settings", ci))
            result = "系统工具";
        else
            result = "其他应用";

        // 写入缓存
        _categoryCache[processName] = result;
        return result;
    }
}
