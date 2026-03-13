namespace RecordTime.Core.Services;

/// <summary>
/// 媒体检测服务接口
/// </summary>
public interface IMediaDetector
{
    /// <summary>
    /// 开始监控媒体播放
    /// </summary>
    void Start();

    /// <summary>
    /// 停止监控
    /// </summary>
    void Stop();

    /// <summary>
    /// 检查是否有媒体正在播放
    /// </summary>
    bool IsMediaPlaying();

    /// <summary>
    /// 检查指定进程是否在播放媒体
    /// </summary>
    bool IsProcessPlayingMedia(int processId);

    /// <summary>
    /// 检查是否有音频活动
    /// </summary>
    bool HasAudioActivity();

    /// <summary>
    /// 获取当前播放的媒体信息
    /// </summary>
    MediaInfo? GetCurrentMediaInfo();

    /// <summary>
    /// 检查进程是否为已知的视频播放器
    /// </summary>
    bool IsVideoPlayer(string processName);

    /// <summary>
    /// 检查进程是否为浏览器
    /// </summary>
    bool IsBrowser(string processName);

    /// <summary>
    /// 检查进程是否为音乐播放器
    /// </summary>
    bool IsMusicPlayer(string processName);

    /// <summary>
    /// 检查是否有音乐播放器正在运行（后台音乐检测）
    /// </summary>
    bool IsMusicPlayerRunning();

    /// <summary>
    /// 检查是否有视频播放器正在运行
    /// </summary>
    bool IsVideoPlayerRunning();
}

/// <summary>
/// 媒体信息
/// </summary>
public class MediaInfo
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public bool IsPlaying { get; set; }
    public string SourceAppName { get; set; } = string.Empty;
}
