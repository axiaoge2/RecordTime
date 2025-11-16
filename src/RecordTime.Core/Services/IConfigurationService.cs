namespace RecordTime.Core.Services;

/// <summary>
/// 配置服务接口
/// </summary>
public interface IConfigurationService
{
    /// <summary>
    /// 获取当前应用配置
    /// </summary>
    AppConfiguration Current { get; }

    /// <summary>
    /// 重新加载配置
    /// </summary>
    void Reload();
}
