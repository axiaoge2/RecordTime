namespace RecordTime.Core.Models;

/// <summary>
/// AI 目标建议 - 缓存 AI 生成的时间预算建议
/// </summary>
public class GoalSuggestion
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 应用进程名（如 "WeChat"），与 Category 二选一
    /// </summary>
    public string? ProcessName { get; set; }

    /// <summary>
    /// 分类名（如 "视频娱乐"），与 ProcessName 二选一
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 建议的目标类型
    /// </summary>
    public BudgetType SuggestedType { get; set; }

    /// <summary>
    /// 建议的目标分钟数
    /// </summary>
    public int SuggestedMinutes { get; set; }

    /// <summary>
    /// AI 生成的建议理由
    /// </summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>
    /// 建议优先级（1-5，5最高）
    /// </summary>
    public int Priority { get; set; } = 3;

    /// <summary>
    /// 基于的历史数据天数
    /// </summary>
    public int BasedOnDays { get; set; }

    /// <summary>
    /// 历史平均使用分钟数
    /// </summary>
    public int HistoricalAverageMinutes { get; set; }

    /// <summary>
    /// 建议生成时间
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 建议过期时间（默认7天后过期）
    /// </summary>
    public DateTime ExpiresAt { get; set; } = DateTime.Now.AddDays(7);

    /// <summary>
    /// 用户是否已处理（接受/拒绝/忽略）
    /// </summary>
    public bool IsProcessed { get; set; } = false;

    /// <summary>
    /// 用户处理结果（Accepted/Rejected/Ignored）
    /// </summary>
    public string? ProcessResult { get; set; }

    /// <summary>
    /// 建议是否已过期
    /// </summary>
    public bool IsExpired => DateTime.Now > ExpiresAt;

    /// <summary>
    /// 是否为应用建议
    /// </summary>
    public bool IsAppSuggestion => !string.IsNullOrEmpty(ProcessName);

    /// <summary>
    /// 是否为分类建议
    /// </summary>
    public bool IsCategorySuggestion => !string.IsNullOrEmpty(Category);
}
