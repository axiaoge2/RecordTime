namespace RecordTime.Core.Models;

/// <summary>
/// 每日预算进度 - 追踪每个时间预算的每日执行情况
/// </summary>
public class DailyBudgetProgress
{
    /// <summary>
    /// 主键ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// 关联的时间预算ID
    /// </summary>
    public int TimeBudgetId { get; set; }

    /// <summary>
    /// 关联的时间预算
    /// </summary>
    public TimeBudget? TimeBudget { get; set; }

    /// <summary>
    /// 日期（仅日期部分）
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 实际使用分钟数
    /// </summary>
    public int ActualMinutes { get; set; }

    /// <summary>
    /// 目标分钟数（快照，防止目标修改影响历史记录）
    /// </summary>
    public int TargetMinutes { get; set; }

    /// <summary>
    /// 是否已发送提醒
    /// </summary>
    public bool ReminderSent { get; set; } = false;

    /// <summary>
    /// 是否已超标/达标
    /// </summary>
    public bool GoalReached { get; set; } = false;

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdated { get; set; } = DateTime.Now;

    /// <summary>
    /// 完成百分比
    /// </summary>
    public double ProgressPercentage => TargetMinutes > 0
        ? Math.Min(100.0, (double)ActualMinutes / TargetMinutes * 100)
        : 0;

    /// <summary>
    /// 剩余分钟数（上限目标）或还需分钟数（下限目标）
    /// </summary>
    public int RemainingMinutes => Math.Max(0, TargetMinutes - ActualMinutes);

    /// <summary>
    /// 是否超出上限（仅对 Maximum 类型有意义）
    /// </summary>
    public bool IsOverBudget => ActualMinutes > TargetMinutes;
}
