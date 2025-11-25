using System;

namespace RecordTime.Core.Models;

/// <summary>
/// 每日使用统计数据模型
/// </summary>
public class DailyUsage
{
    /// <summary>
    /// 日期
    /// </summary>
    public DateTime Date { get; set; }

    /// <summary>
    /// 总使用时长
    /// </summary>
    public TimeSpan TotalDuration { get; set; }

    /// <summary>
    /// 总时长（小时，用于图表显示）
    /// </summary>
    public double TotalHours => TotalDuration.TotalHours;

    /// <summary>
    /// 格式化的日期字符串（用于图表 X 轴）
    /// </summary>
    public string DateLabel => Date.ToString("MM/dd");

    /// <summary>
    /// 格式化的时长字符串
    /// </summary>
    public string DurationLabel
    {
        get
        {
            var hours = (int)TotalDuration.TotalHours;
            var minutes = TotalDuration.Minutes;
            if (hours > 0)
            {
                return $"{hours}h {minutes}m";
            }
            return $"{minutes}m";
        }
    }
}
