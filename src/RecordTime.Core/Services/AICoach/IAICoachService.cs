using RecordTime.Core.Models.AICoach;

namespace RecordTime.Core.Services.AICoach;

/// <summary>
/// AI Coach 服务接口
/// </summary>
public interface IAICoachService
{
    /// <summary>
    /// 发送快捷操作请求
    /// </summary>
    Task<AICoachResponse> SendQuickActionAsync(CognitiveContext context, QuickActionType actionType);

    /// <summary>
    /// 发送自由对话请求
    /// </summary>
    Task<AICoachResponse> SendChatAsync(CognitiveContext context, string userMessage);

    /// <summary>
    /// 检查服务是否可用
    /// </summary>
    Task<bool> IsAvailableAsync();
}
