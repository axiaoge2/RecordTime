namespace RecordTime.Core.Models.AI;

/// <summary>
/// AI 分析结果
/// </summary>
public class AIAnalysisResult
{
    /// <summary>
    /// 整体评价摘要
    /// </summary>
    public string Summary { get; set; } = string.Empty;

    /// <summary>
    /// 关键洞察列表
    /// </summary>
    public List<string> Insights { get; set; } = new();

    /// <summary>
    /// 发现的问题
    /// </summary>
    public List<string> Issues { get; set; } = new();

    /// <summary>
    /// 改进建议
    /// </summary>
    public List<AIRecommendation> Recommendations { get; set; } = new();

    /// <summary>
    /// 效率评分 (0-100)
    /// </summary>
    public int ProductivityScore { get; set; }

    /// <summary>
    /// 分析完成时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// 使用的模型名称
    /// </summary>
    public string ModelUsed { get; set; } = string.Empty;

    /// <summary>
    /// 分析是否成功
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// 错误信息（如果失败）
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// 创建失败结果
    /// </summary>
    public static AIAnalysisResult Failure(string errorMessage)
    {
        return new AIAnalysisResult
        {
            IsSuccess = false,
            ErrorMessage = errorMessage,
            AnalyzedAt = DateTime.Now
        };
    }

    /// <summary>
    /// 创建成功结果
    /// </summary>
    public static AIAnalysisResult Success(
        string summary,
        List<string> insights,
        List<string> issues,
        List<AIRecommendation> recommendations,
        int productivityScore,
        string modelUsed)
    {
        return new AIAnalysisResult
        {
            IsSuccess = true,
            Summary = summary,
            Insights = insights,
            Issues = issues,
            Recommendations = recommendations,
            ProductivityScore = productivityScore,
            ModelUsed = modelUsed,
            AnalyzedAt = DateTime.Now
        };
    }
}

/// <summary>
/// AI 改进建议
/// </summary>
public class AIRecommendation
{
    /// <summary>
    /// 建议标题
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 详细描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 优先级
    /// </summary>
    public AIPriority Priority { get; set; } = AIPriority.Medium;
}

/// <summary>
/// 建议优先级
/// </summary>
public enum AIPriority
{
    High,
    Medium,
    Low
}
