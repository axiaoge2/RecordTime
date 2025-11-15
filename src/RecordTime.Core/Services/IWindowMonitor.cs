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
    /// 窗口焦点变化事件
    /// </summary>
    event EventHandler<WindowInfo>? WindowFocusChanged;
}
