namespace RecordTime.Core.Models.AI;

/// <summary>
/// AI 服务配置
/// </summary>
public class AIConfig
{
    /// <summary>
    /// 配置名称（用于多配置切换）
    /// </summary>
    public string Name { get; set; } = "默认配置";

    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// API 基础地址
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    /// <summary>
    /// 隐私级别
    /// </summary>
    public AIPrivacyLevel PrivacyLevel { get; set; } = AIPrivacyLevel.CategoryOnly;

    /// <summary>
    /// 是否为默认配置
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// 创建时间
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>
    /// 最后使用时间
    /// </summary>
    public DateTime LastUsedAt { get; set; } = DateTime.Now;
}
