namespace RecordTime.Core.Models;

/// <summary>
/// 时间预算目标类型
/// </summary>
public enum BudgetType
{
    /// <summary>
    /// 上限目标 - 不超过指定时长
    /// </summary>
    Maximum,

    /// <summary>
    /// 下限目标 - 至少达到指定时长
    /// </summary>
    Minimum
}
