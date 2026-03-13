using RecordTime.Core.Models;

namespace RecordTime.Core.Services;

/// <summary>
/// 窗口监控服务接口
/// </summary>
public interface IWindowMonitor
{
    /// <summary>
    /// 开始监控
    /// </summary>
    void Start();

    /// <summary>
    /// 停止监控
    /// </summary>
    void Stop();

    /// <summary>
    /// 获取当前前台窗口信息
    /// </summary>
    WindowInfo? GetForegroundWindow();

    /// <summary>
    /// 获取指定时间范围内的应用切换次数
    /// </summary>
    /// <param name="minutes">时间范围（分钟）</param>
    /// <returns>切换次数</returns>
    int GetSwitchCount(int minutes);

    /// <summary>
    /// 获取切换频率统计
    /// </summary>
    /// <returns>包含各时间窗口切换次数的统计</returns>
    SwitchFrequencyStats GetSwitchStats();

    /// <summary>
    /// 窗口焦点变化事件
    /// </summary>
    event EventHandler<WindowInfo>? WindowFocusChanged;
}
