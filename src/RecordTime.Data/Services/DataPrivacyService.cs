using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RecordTime.Data.Services;

/// <summary>
/// 数据隐私保护服务
/// </summary>
public class DataPrivacyService
{
    private static readonly string Salt = GenerateSalt();

    private static readonly Regex UrlRegex = new(@"https?://[^\s]+", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(@"[\w\.-]+@[\w\.-]+\.\w+", RegexOptions.Compiled);
    private static readonly Regex IpRegex = new(@"\b\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}\b", RegexOptions.Compiled);

    [ThreadStatic]
    private static SHA256? t_sha256;

    public static string HashWindowTitle(string title)
    {
        if (string.IsNullOrEmpty(title))
            return string.Empty;

        var sanitized = RemoveSensitiveInfo(title);

        var sha256 = t_sha256 ??= SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(sanitized + Salt);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hash);
    }

    private static string RemoveSensitiveInfo(string text)
    {
        text = UrlRegex.Replace(text, "[URL]");
        text = EmailRegex.Replace(text, "[EMAIL]");
        text = IpRegex.Replace(text, "[IP]");
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
