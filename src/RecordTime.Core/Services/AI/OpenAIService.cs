using System.ClientModel;
using System.Text.Json;
using OpenAI;
using OpenAI.Chat;
using RecordTime.Core.Models.AI;
using Serilog;

namespace RecordTime.Core.Services.AI;

/// <summary>
/// OpenAI 云端 AI 分析服务
/// </summary>
public class OpenAIService : IAIAnalysisService
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private ChatClient? _client;

    public string ServiceName => "OpenAI";
    public string ServiceDescription => "使用 OpenAI GPT 模型进行智能分析";

    public bool IsAvailable => !string.IsNullOrWhiteSpace(_apiKey);

    /// <summary>
    /// 创建 OpenAI 服务实例
    /// </summary>
    /// <param name="apiKey">API Key</param>
    /// <param name="model">模型名称，默认 gpt-4o-mini</param>
    /// <param name="baseUrl">API 基础 URL（支持兼容 API）</param>
    public OpenAIService(string apiKey, string model = "gpt-4o-mini", string? baseUrl = null)
    {
        _apiKey = apiKey;
        _model = model;
        _baseUrl = baseUrl ?? "https://api.openai.com/v1";

        if (IsAvailable)
        {
            InitializeClient();
        }
    }

    private void InitializeClient()
    {
        try
        {
            var credential = new ApiKeyCredential(_apiKey);
            var options = new OpenAIClientOptions
            {
                Endpoint = new Uri(_baseUrl)
            };

            var openAiClient = new OpenAIClient(credential, options);
            _client = openAiClient.GetChatClient(_model);

            Log.Debug("OpenAI 客户端初始化成功，模型: {Model}", _model);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "OpenAI 客户端初始化失败");
            _client = null;
        }
    }

    public async Task<(bool IsValid, string? ErrorMessage)> ValidateConfigurationAsync()
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return (false, "API Key 未配置");
        }

        if (_client == null)
        {
            InitializeClient();
            if (_client == null)
            {
                return (false, "客户端初始化失败");
            }
        }

        try
        {
            // 发送一个简单请求测试连接
            var messages = new List<ChatMessage>
            {
                new UserChatMessage("Hello")
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 10
            };

            var response = await _client.CompleteChatAsync(messages, options);

            if (response?.Value != null)
            {
                Log.Information("OpenAI 配置验证成功");
                return (true, null);
            }

            return (false, "API 响应异常");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "OpenAI 配置验证失败");
            return (false, $"连接失败: {ex.Message}");
        }
    }

    public async Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisInput input, CancellationToken cancellationToken = default)
    {
        if (!IsAvailable || _client == null)
        {
            return AIAnalysisResult.Failure("OpenAI 服务未配置或不可用");
        }

        try
        {
            Log.Information("开始 AI 分析，时间范围: {Start} - {End}", input.StartDate, input.EndDate);

            var prompt = BuildPrompt(input);
            var systemPrompt = BuildSystemPrompt();

            var messages = new List<ChatMessage>
            {
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(prompt)
            };

            var options = new ChatCompletionOptions
            {
                MaxOutputTokenCount = 2000,
                Temperature = 0.7f
            };

            var response = await _client.CompleteChatAsync(messages, options, cancellationToken);

            if (response?.Value == null)
            {
                return AIAnalysisResult.Failure("AI 响应为空");
            }

            var content = response.Value.Content[0].Text;
            Log.Debug("AI 原始响应: {Content}", content);

            var result = ParseResponse(content);
            result.ModelUsed = _model;

            Log.Information("AI 分析完成，效率评分: {Score}", result.ProductivityScore);
            return result;
        }
        catch (OperationCanceledException)
        {
            Log.Warning("AI 分析被取消");
            return AIAnalysisResult.Failure("分析已取消");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AI 分析失败");
            return AIAnalysisResult.Failure($"分析失败: {ex.Message}");
        }
    }

    private string BuildSystemPrompt()
    {
        return @"你是一个专业的时间管理顾问和效率分析师。你的任务是分析用户的电脑使用数据，提供有价值的洞察和改进建议。

分析时请注意：
1. 根据分类和活动类型判断用户的工作效率
2. 识别可能的时间浪费模式
3. 提供具体、可执行的改进建议
4. 评分要客观，不要过于苛刻或宽松
5. 建议要考虑实际可行性

你必须以 JSON 格式回复，格式如下：
{
  ""summary"": ""2-3句话的整体评价"",
  ""insights"": [""洞察1"", ""洞察2"", ""洞察3""],
  ""issues"": [""问题1"", ""问题2""],
  ""recommendations"": [
    {""title"": ""建议标题"", ""description"": ""详细描述"", ""priority"": ""High/Medium/Low""}
  ],
  ""productivityScore"": 75
}";
    }

    private string BuildPrompt(AIAnalysisInput input)
    {
        var categoryBreakdown = string.Join("\n", input.CategoryHours
            .OrderByDescending(kv => kv.Value)
            .Select(kv => $"- {kv.Key}: {kv.Value:F1} 小时 ({kv.Value / input.TotalActiveHours * 100:F1}%)"));

        var activityBreakdown = string.Join("\n", input.ActivityHours
            .OrderByDescending(kv => kv.Value)
            .Select(kv => $"- {GetActivityTypeName(kv.Key)}: {kv.Value:F1} 小时"));

        var dailyTrend = input.DailyHours.Count > 0
            ? string.Join("\n", input.DailyHours.Select(kv => $"- {kv.Key}: {kv.Value:F1} 小时"))
            : "无数据";

        return $@"请分析以下用户时间使用数据：

## 基本信息
- 分析时段: {input.StartDate:yyyy年MM月dd日} 至 {input.EndDate:yyyy年MM月dd日}
- 总活跃时间: {input.TotalActiveHours:F1} 小时
- 使用应用数: {input.UniqueAppCount} 个
- 会话次数: {input.SessionCount} 次
- 平均会话时长: {input.AvgSessionMinutes:F1} 分钟
- 最活跃时段: {input.PeakHour}:00 - {input.PeakHour + 1}:00

## 分类使用时长
{categoryBreakdown}

## 活动类型分布
{activityBreakdown}

## 每日使用趋势
{dailyTrend}

请根据以上数据，分析用户的时间使用效率，识别问题，并提供改进建议。";
    }

    private string GetActivityTypeName(string activityType)
    {
        return activityType switch
        {
            "Video" => "视频观看",
            "Gaming" => "游戏",
            "ActiveTyping" => "主动输入（工作/编程）",
            "PassiveBrowsing" => "被动浏览",
            "Idle" => "空闲",
            _ => activityType
        };
    }

    private AIAnalysisResult ParseResponse(string content)
    {
        try
        {
            // 尝试提取 JSON 部分（处理可能的 markdown 代码块）
            var jsonContent = ExtractJson(content);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var parsed = JsonSerializer.Deserialize<AIResponseDto>(jsonContent, options);

            if (parsed == null)
            {
                return AIAnalysisResult.Failure("无法解析 AI 响应");
            }

            var recommendations = parsed.Recommendations?.Select(r => new AIRecommendation
            {
                Title = r.Title ?? "",
                Description = r.Description ?? "",
                Priority = ParsePriority(r.Priority)
            }).ToList() ?? new List<AIRecommendation>();

            return AIAnalysisResult.Success(
                summary: parsed.Summary ?? "分析完成",
                insights: parsed.Insights ?? new List<string>(),
                issues: parsed.Issues ?? new List<string>(),
                recommendations: recommendations,
                productivityScore: Math.Clamp(parsed.ProductivityScore, 0, 100),
                modelUsed: _model
            );
        }
        catch (JsonException ex)
        {
            Log.Warning(ex, "JSON 解析失败，尝试提取关键信息");

            // 回退：尝试从文本中提取基本信息
            return new AIAnalysisResult
            {
                IsSuccess = true,
                Summary = content.Length > 200 ? content.Substring(0, 200) + "..." : content,
                Insights = new List<string> { "AI 分析已完成，但格式解析异常" },
                Issues = new List<string>(),
                Recommendations = new List<AIRecommendation>(),
                ProductivityScore = 70,
                ModelUsed = _model,
                AnalyzedAt = DateTime.Now
            };
        }
    }

    private string ExtractJson(string content)
    {
        // 移除可能的 markdown 代码块标记
        content = content.Trim();

        if (content.StartsWith("```json"))
        {
            content = content.Substring(7);
        }
        else if (content.StartsWith("```"))
        {
            content = content.Substring(3);
        }

        if (content.EndsWith("```"))
        {
            content = content.Substring(0, content.Length - 3);
        }

        // 找到 JSON 对象的开始和结束
        var startIndex = content.IndexOf('{');
        var endIndex = content.LastIndexOf('}');

        if (startIndex >= 0 && endIndex > startIndex)
        {
            content = content.Substring(startIndex, endIndex - startIndex + 1);
        }

        return content.Trim();
    }

    private AIPriority ParsePriority(string? priority)
    {
        return priority?.ToLower() switch
        {
            "high" => AIPriority.High,
            "low" => AIPriority.Low,
            _ => AIPriority.Medium
        };
    }

    /// <summary>
    /// AI 响应的 DTO（用于 JSON 反序列化）
    /// </summary>
    private class AIResponseDto
    {
        public string? Summary { get; set; }
        public List<string>? Insights { get; set; }
        public List<string>? Issues { get; set; }
        public List<RecommendationDto>? Recommendations { get; set; }
        public int ProductivityScore { get; set; }
    }

    private class RecommendationDto
    {
        public string? Title { get; set; }
        public string? Description { get; set; }
        public string? Priority { get; set; }
    }
}
