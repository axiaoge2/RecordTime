namespace RecordTime.Core.Models;

/// <summary>
/// 时间预算 - 定义应用或分类的时间目标
/// </summary>
public class TimeBudget
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
    /// 显示名称（用于 UI 展示）
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// 目标类型（上限/下限）
    /// </summary>
    public BudgetType Type { get; set; }

    /// <summary>
    /// 目标时长（分钟）
    /// </summary>
    public int TargetMinutes { get; set; }

    /// <summary>
    /// 是否启用提醒
    /// </summary>
    public bool ReminderEnabled { get; set; } = true;

    /// <summary>
    /// 提醒阈值（百分比，默认 80）
    /// </summary>
    public int ReminderThreshold { get; set; } = 80;

    /// <summary>
    /// 是否启用此目标
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 更新时间
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 目标时长（TimeSpan 格式）
    /// </summary>
    public TimeSpan TargetDuration => TimeSpan.FromMinutes(TargetMinutes);

    /// <summary>
    /// 判断是否为应用目标
    /// </summary>
    public bool IsAppBudget => !string.IsNullOrEmpty(ProcessName);

    /// <summary>
    /// 判断是否为分类目标
    /// </summary>
    public bool IsCategoryBudget => !string.IsNullOrEmpty(Category);
}
