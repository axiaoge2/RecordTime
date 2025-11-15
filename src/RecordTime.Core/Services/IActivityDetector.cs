using RecordTime.Core.Models;

namespace RecordTime.Core.Services;

/// <summary>
/// 活动检测服务接口
/// </summary>
public interface IActivityDetector
{
    /// <summary>
    /// 判定当前活动类型
    /// </summary>
    ActivityType DetermineActivity(WindowInfo window, SystemState state);

    /// <summary>
    /// 检测视频播放状态
    /// </summary>
    bool IsVideoPlaying(string processName);

    /// <summary>
    /// 获取应用分类
    /// </summary>
    string GetAppCategory(string processName);
}
