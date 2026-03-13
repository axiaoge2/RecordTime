namespace RecordTime.Core.Models;

/// <summary>
/// 应用切换频率统计
/// </summary>
public struct SwitchFrequencyStats
{
    /// <summary>
    /// 最近1分钟切换次数
    /// </summary>
    public int SwitchesLast1Min { get; set; }

    /// <summary>
    /// 最近5分钟切换次数
    /// </summary>
    public int SwitchesLast5Min { get; set; }

    /// <summary>
    /// 最近10分钟切换次数
    /// </summary>
    public int SwitchesLast10Min { get; set; }

    /// <summary>
    /// 判断是否为注意力碎片化状态
    /// 标准：5分钟内切换超过10次，平均每30秒切换一次
    /// </summary>
    public bool IsFragmented => SwitchesLast5Min > 10;

    /// <summary>
    /// 判断是否为深度专注状态
    /// 标准：5分钟内切换少于3次
    /// </summary>
    public bool IsDeepFocus => SwitchesLast5Min < 3;
}
