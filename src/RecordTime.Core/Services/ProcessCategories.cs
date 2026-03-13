namespace RecordTime.Core.Services;

/// <summary>
/// 共享的进程分类列表，供 ActivityDetector 和 MediaDetector 统一引用
/// </summary>
public static class ProcessCategories
{
    public static readonly HashSet<string> VideoPlayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "vlc", "potplayer", "potplayermini", "potplayer64",
        "mpv", "mpc-hc", "mpc-hc64", "mpc-be", "mpc-be64",
        "wmplayer", "k-litecodecpackx64", "kmplayer",
        "gomplayer", "daum", "smplayer", "kodi",
        "netflix", "primevideo", "disney", "disneyplus",
        "hulu", "hbomax", "appletv",
        "bilibili", "bilibiliuwp", "tencent_video", "qqlivetv",
        "iqiyi", "iqiyiuwp", "youku", "manggotv",
        "douyu", "huya", "kuaishou", "douyin"
    };

    public static readonly HashSet<string> Browsers = new(StringComparer.OrdinalIgnoreCase)
    {
        "chrome", "msedge", "firefox", "brave", "opera", "vivaldi",
        "chromium", "edge", "iexplore", "safari",
        "maxthon", "360se", "360chrome", "sogouexplorer",
        "qqbrowser", "liebao", "tor"
    };

    public static readonly HashSet<string> OnlineMeetingApps = new(StringComparer.OrdinalIgnoreCase)
    {
        "zoom", "teams", "skype", "webex", "dingtalk", "lark",
        "feishu", "腾讯会议", "tencent_meeting", "voovmeeting",
        "googlemeet", "meet", "discord"
    };

    public static readonly HashSet<string> MusicPlayers = new(StringComparer.OrdinalIgnoreCase)
    {
        "spotify", "cloudmusic", "qqmusic", "kugou", "kuwo",
        "foobar2000", "aimp", "musicbee", "netease_cloud_music",
        "applemusic", "itunes", "wmplayer"
    };

    public static readonly HashSet<string> DesktopProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "explorer", "progman", "workerw"
    };

    /// <summary>
    /// MediaDetector 额外关注的视频相关进程（含会议和录屏软件）
    /// </summary>
    public static readonly HashSet<string> VideoRelatedProcesses = new(StringComparer.OrdinalIgnoreCase)
    {
        "zoom", "teams", "skype",
        "obs", "obs64", "streamlabs"
    };
}
