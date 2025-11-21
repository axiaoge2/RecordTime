using RecordTime.Core.Models.AI;

namespace RecordTime.Core.Services.AI;

/// <summary>
/// AI 分析服务接口
/// </summary>
public interface IAIAnalysisService
{
    /// <summary>
    /// 分析用户时间使用数据
    /// </summary>
    /// <param name="input">脱敏后的分析输入数据</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>分析结果</returns>
    Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisInput input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 服务是否可用
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 服务名称
    /// </summary>
    string ServiceName { get; }

    /// <summary>
    /// 服务描述
    /// </summary>
    string ServiceDescription { get; }

    /// <summary>
    /// 验证服务配置是否正确
    /// </summary>
    /// <returns>验证结果和错误信息</returns>
    Task<(bool IsValid, string? ErrorMessage)> ValidateConfigurationAsync();
}
