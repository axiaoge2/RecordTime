namespace RecordTime.Core.Models;

/// <summary>
/// 活动类型枚举
/// </summary>
public enum ActivityType
{
    /// <summary>
    /// 视频播放（最高优先级）
    /// </summary>
    Video,

    /// <summary>
    /// 主动输入（编程、写作等）
    /// </summary>
    ActiveTyping,

    /// <summary>
    /// 游戏
    /// </summary>
    Gaming,

    /// <summary>
    /// 在线会议/通话（Zoom、Teams、腾讯会议等）
    /// </summary>
    Meeting,

    /// <summary>
    /// 阅读浏览（高滚轮活动 + 低键盘活动，表示专注阅读）
    /// </summary>
    Reading,

    /// <summary>
    /// 被动浏览
    /// </summary>
    PassiveBrowsing,

    /// <summary>
    /// 空闲状态
    /// </summary>
    Idle
}
