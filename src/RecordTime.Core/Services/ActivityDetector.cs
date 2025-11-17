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

        // 优先级3: 游戏检测
        if (window.IsFullscreen && state.FrequentInput)
        {
            // 全屏且频繁输入，很可能是游戏
            return ActivityType.Gaming;
        }

        string processLower = window.ProcessName.ToLowerInvariant();
        if ((processLower.Contains("game") || processLower.Contains("steam") ||
             processLower.Contains("epic") || processLower.Contains("origin")) &&
            state.FrequentInput)
        {
            return ActivityType.Gaming;
        }

        // 优先级4: 主动交互（提高阈值，更准确判断）
        if (state.KeyboardActivityLast30s > 20 ||
            (state.KeyboardActivityLast30s > 10 && state.MouseClicksLast30s > 5))
        {
            return ActivityType.ActiveTyping;
        }

        // 优先级5: 被动浏览
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

    /// <summary>
    /// 根据进程名获取应用分类
    /// </summary>
    public string GetAppCategory(string processName)
    {
        processName = processName.ToLowerInvariant();

        // 视频娱乐
        if (VideoAppProcesses.Contains(processName))
            return "视频娱乐";

        // 浏览器
        if (BrowserProcesses.Contains(processName))
            return "浏览器";

        // 在线会议
        if (OnlineMeetingApps.Contains(processName))
            return "在线会议";

        // 音乐
        if (MusicPlayerApps.Contains(processName))
            return "音乐";

        // 桌面
        if (DesktopProcesses.Contains(processName))
            return "系统桌面";

        // 开发工具
        if (processName.Contains("code") || processName.Contains("studio") ||
            processName.Contains("idea") || processName.Contains("eclipse") ||
            processName.Contains("rider") || processName.Contains("pycharm") ||
            processName.Contains("webstorm") || processName.Contains("cursor") ||
            processName.Contains("sublime") || processName.Contains("vim") ||
            processName.Contains("emacs") || processName.Contains("atom"))
            return "开发工具";

        // 办公软件
        if (processName.Contains("word") || processName.Contains("excel") ||
            processName.Contains("powerpoint") || processName.Contains("notion") ||
            processName.Contains("onenote") || processName.Contains("outlook") ||
            processName.Contains("winword") || processName.Contains("wps") ||
            processName.Contains("evernote") || processName.Contains("obsidian"))
            return "办公软件";

        // 社交通讯
        if (processName.Contains("wechat") || processName.Contains("qq") ||
            processName.Contains("telegram") || processName.Contains("discord") ||
            processName.Contains("slack") || processName.Contains("teams") ||
            processName.Contains("zoom") || processName.Contains("dingtalk") ||
            processName.Contains("feishu") || processName.Contains("skype"))
            return "社交通讯";

        // 游戏
        if (processName.Contains("game") || processName.Contains("steam") ||
            processName.Contains("origin") || processName.Contains("epic") ||
            processName.Contains("league") || processName.Contains("dota") ||
            processName.Contains("minecraft") || processName.Contains("genshin") ||
            processName.Contains("yuanshen") || processName.Contains("wegame"))
            return "游戏";

        // 音乐
        if (processName.Contains("spotify") || processName.Contains("music") ||
            processName.Contains("cloudmusic") || processName.Contains("qqmusic") ||
            processName.Contains("foobar") || processName.Contains("aimp"))
            return "音乐";

        // 图形设计
        if (processName.Contains("photoshop") || processName.Contains("illustrator") ||
            processName.Contains("figma") || processName.Contains("sketch") ||
            processName.Contains("blender") || processName.Contains("gimp"))
            return "图形设计";

        // 系统工具
        if (processName.Contains("terminal") || processName.Contains("cmd") ||
            processName.Contains("powershell") || processName.Contains("explorer") ||
            processName.Contains("taskmgr") || processName.Contains("settings"))
            return "系统工具";

        return "其他应用";
    }
}
