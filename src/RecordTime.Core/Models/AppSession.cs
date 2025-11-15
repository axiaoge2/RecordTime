namespace RecordTime.Core.Models;

/// <summary>
/// 应用使用会话记录
/// </summary>
public class AppSession
{
    public int Id { get; set; }

    /// <summary>
    /// 应用进程名称
    /// </summary>
    public string ProcessName { get; set; } = string.Empty;

    /// <summary>
    /// 应用显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 窗口标题（隐私哈希处理）
    /// </summary>
    public string? WindowTitleHash { get; set; }

    /// <summary>
    /// 活动类型
    /// </summary>
    public ActivityType ActivityType { get; set; }

    /// <summary>
    /// 会话开始时间
    /// </summary>
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 会话结束时间
    /// </summary>
    public DateTime? EndTime { get; set; }

    /// <summary>
    /// 持续时长（秒）
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// 置信度（0-100）
    /// </summary>
    public int Confidence { get; set; }

    /// <summary>
    /// 应用分类（生产力、娱乐、社交等）
    /// </summary>
    public string? Category { get; set; }
}
