namespace RecordTime.Core.Models;

/// <summary>
/// 系统状态信息
/// </summary>
public class SystemState
{
    /// <summary>
    /// 媒体会话是否正在播放
    /// </summary>
    public bool MediaSessionPlaying { get; set; }

    /// <summary>
    /// 是否为已知视频应用
    /// </summary>
    public bool IsVideoApp { get; set; }

    /// <summary>
    /// 音频是否活跃
    /// </summary>
    public bool AudioActive { get; set; }

    /// <summary>
    /// 是否为浏览器
    /// </summary>
    public bool IsBrowser { get; set; }

    /// <summary>
    /// 浏览器中是否有视频播放
    /// </summary>
    public bool BrowserVideoPlaying { get; set; }

    /// <summary>
    /// GPU使用率是否高
    /// </summary>
    public bool HighGpuUsage { get; set; }

    /// <summary>
    /// 最近30秒键盘活动次数
    /// </summary>
    public int KeyboardActivityLast30s { get; set; }

    /// <summary>
    /// 最近30秒鼠标点击次数
    /// </summary>
    public int MouseClicksLast30s { get; set; }

    /// <summary>
    /// 窗口是否聚焦
    /// </summary>
    public bool WindowFocused { get; set; }

    /// <summary>
    /// 系统是否空闲
    /// </summary>
    public bool SystemIdle { get; set; }

    /// <summary>
    /// 是否有频繁输入
    /// </summary>
    public bool FrequentInput { get; set; }
}
