namespace RecordTime.Core.Models.AICoach;

/// <summary>
/// AI Coach 响应
/// </summary>
public class AICoachResponse
{
    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// AI 回复内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 请求类型
    /// </summary>
    public AICoachRequestType RequestType { get; set; }

    /// <summary>
    /// 响应时间
    /// </summary>
    public DateTime ResponseTime { get; set; } = DateTime.Now;

    /// <summary>
    /// API 响应耗时（毫秒）
    /// </summary>
    public int? LatencyMs { get; set; }

    /// <summary>
    /// Token 使用量（如果 API 返回）
    /// </summary>
    public TokenUsage? TokenUsage { get; set; }

    /// <summary>
    /// 创建成功响应
    /// </summary>
    public static AICoachResponse Ok(string content, AICoachRequestType requestType)
    {
        return new AICoachResponse
        {
            Success = true,
            Content = content,
            RequestType = requestType
        };
    }

    /// <summary>
    /// 创建失败响应
    /// </summary>
    public static AICoachResponse Fail(string errorMessage, AICoachRequestType requestType)
    {
        return new AICoachResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            RequestType = requestType
        };
    }
}

/// <summary>
/// AI Coach 请求类型
/// </summary>
public enum AICoachRequestType
{
    /// <summary>
    /// 今日总结
    /// </summary>
    DailySummary,

    /// <summary>
    /// 即时建议
    /// </summary>
    QuickAdvice,

    /// <summary>
    /// 明日规划
    /// </summary>
    TomorrowPlan,

    /// <summary>
    /// 自由对话
    /// </summary>
    FreeChat
}

/// <summary>
/// Token 使用量
/// </summary>
public class TokenUsage
{
    /// <summary>
    /// 输入 Token 数
    /// </summary>
    public int PromptTokens { get; set; }

    /// <summary>
    /// 输出 Token 数
    /// </summary>
    public int CompletionTokens { get; set; }

    /// <summary>
    /// 总 Token 数
    /// </summary>
    public int TotalTokens => PromptTokens + CompletionTokens;
}

/// <summary>
/// AI Coach 对话消息
/// </summary>
public class AICoachMessage
{
    /// <summary>
    /// 消息角色
    /// </summary>
    public MessageRole Role { get; set; }

    /// <summary>
    /// 消息内容
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// 消息时间
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;

    /// <summary>
    /// 请求类型（仅用户消息有效）
    /// </summary>
    public AICoachRequestType? RequestType { get; set; }
}

/// <summary>
/// 消息角色
/// </summary>
public enum MessageRole
{
    /// <summary>
    /// 用户
    /// </summary>
    User,

    /// <summary>
    /// AI 助手
    /// </summary>
    Assistant,

    /// <summary>
    /// 系统
    /// </summary>
    System
}
