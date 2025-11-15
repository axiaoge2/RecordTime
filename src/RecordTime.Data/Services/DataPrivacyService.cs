using System.Security.Cryptography;
using System.Text;

namespace RecordTime.Data.Services;

/// <summary>
/// 数据隐私保护服务
/// </summary>
public class DataPrivacyService
{
    private static readonly string Salt = GenerateSalt();

    /// <summary>
    /// 对窗口标题进行哈希处理（保护隐私）
    /// </summary>
    public static string HashWindowTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        // 移除敏感信息（URL、邮箱等）
        var sanitized = RemoveSensitiveInfo(title);

        // 生成稳定哈希
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(sanitized + Salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    /// <summary>
    /// 移除敏感信息
    /// </summary>
    private static string RemoveSensitiveInfo(string text)
    {
        // 移除URL
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"https?://[^\s]+",
            "[URL]"
        );

        // 移除邮箱
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"[\w\.-]+@[\w\.-]+\.\w+",
            "[EMAIL]"
        );

        // 移除IP地址
        text = System.Text.RegularExpressions.Regex.Replace(
            text,
            @"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b",
            "[IP]"
        );

        return text;
    }

    private static string GenerateSalt()
    {
        // 为当前用户生成唯一salt
        var machineId = Environment.MachineName;
        var userId = Environment.UserName;
        return $"{machineId}_{userId}";
    }

    /// <summary>
    /// 准备用于AI分析的脱敏数据
    /// </summary>
    public static AIAnalysisData PrepareForAI(List<Core.Models.AppSession> sessions)
    {
        var totalTime = sessions.Sum(s => s.DurationSeconds);

        var categoryStats = sessions
            .GroupBy(s => s.Category ?? "未分类")
            .ToDictionary(
                g => g.Key,
                g => TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds))
            );

        var activityStats = sessions
            .GroupBy(s => s.ActivityType)
            .ToDictionary(
                g => g.Key.ToString(),
                g => TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds))
            );

        return new AIAnalysisData
        {
            TotalActiveTime = TimeSpan.FromSeconds(totalTime),
            CategoryBreakdown = categoryStats,
            ActivityBreakdown = activityStats,
            SessionCount = sessions.Count
        };
    }
}

/// <summary>
/// AI分析数据模型（脱敏后）
/// </summary>
public class AIAnalysisData
{
    public TimeSpan TotalActiveTime { get; set; }
    public Dictionary<string, TimeSpan> CategoryBreakdown { get; set; } = new();
    public Dictionary<string, TimeSpan> ActivityBreakdown { get; set; } = new();
    public int SessionCount { get; set; }
}
