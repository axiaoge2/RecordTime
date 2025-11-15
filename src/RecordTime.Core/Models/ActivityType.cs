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
    /// 被动浏览
    /// </summary>
    PassiveBrowsing,

    /// <summary>
    /// 空闲状态
    /// </summary>
    Idle
}
