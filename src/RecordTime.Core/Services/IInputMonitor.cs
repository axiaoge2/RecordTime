namespace RecordTime.Core.Services;

/// <summary>
/// 输入监控服务接口
/// </summary>
public interface IInputMonitor
{
    /// <summary>
    /// 开始监控输入活动
    /// </summary>
    void Start();

    /// <summary>
    /// 停止监控
    /// </summary>
    void Stop();

    /// <summary>
    /// 获取最近N秒内的键盘活动次数
    /// </summary>
    int GetKeyboardActivityCount(int seconds);

    /// <summary>
    /// 获取最近N秒内的鼠标点击次数
    /// </summary>
    int GetMouseClickCount(int seconds);

    /// <summary>
    /// 获取最近N秒内的鼠标移动距离
    /// </summary>
    int GetMouseMovementDistance(int seconds);

    /// <summary>
    /// 获取最近N秒内的鼠标滚轮滚动次数
    /// </summary>
    int GetScrollCount(int seconds);

    /// <summary>
    /// 获取系统空闲时间（秒）
    /// </summary>
    int GetIdleTimeSeconds();

    /// <summary>
    /// 获取最近N秒内的所有输入统计信息
    /// </summary>
    InputActivityStats GetInputStats(int seconds);

    /// <summary>
    /// 用户活动检测事件（用于会话恢复）
    /// </summary>
    event EventHandler? UserActivityDetected;
}

/// <summary>
/// 输入活动统计信息
/// </summary>
public struct InputActivityStats
{
    public int KeyboardActivityCount { get; set; }
    public int MouseClickCount { get; set; }
    public int MouseMovementDistance { get; set; }
    public int ScrollCount { get; set; }
    public int IdleTimeSeconds { get; set; }
}
