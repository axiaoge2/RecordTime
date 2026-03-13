using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using RecordTime.Core.Models.AICoach;
using Serilog;

namespace RecordTime.Core.Services.AICoach;

/// <summary>
/// AI Coach 服务实现 - 支持 OpenAI 兼容的 API
/// </summary>
public class AICoachService : IAICoachService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly PromptBuilder _promptBuilder;
    private readonly AICoachSettings _settings;
    private bool _disposed;

    public AICoachService(PromptBuilder promptBuilder, AICoachSettings settings)
    {
        _promptBuilder = promptBuilder;
        _settings = settings;

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(settings.ApiBaseUrl),
            Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds)
        };

        if (!string.IsNullOrEmpty(settings.ApiKey))
        {
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {settings.ApiKey}");
        }
    }

    /// <inheritdoc />
    public async Task<AICoachResponse> SendQuickActionAsync(CognitiveContext context, QuickActionType actionType)
    {
        var requestType = actionType switch
        {
            QuickActionType.DailySummary => AICoachRequestType.DailySummary,
            QuickActionType.QuickAdvice => AICoachRequestType.QuickAdvice,
            QuickActionType.TomorrowPlan => AICoachRequestType.TomorrowPlan,
            _ => AICoachRequestType.FreeChat
        };

        try
        {
            var systemMessage = _promptBuilder.BuildSystemMessage();
            var userMessage = _promptBuilder.BuildQuickActionMessage(context, actionType);

            return await CallApiAsync(systemMessage, userMessage, requestType);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送快捷操作请求失败: {ActionType}", actionType);
            return AICoachResponse.Fail($"请求失败: {ex.Message}", requestType);
        }
    }

    /// <inheritdoc />
    public async Task<AICoachResponse> SendChatAsync(CognitiveContext context, string userMessage)
    {
        try
        {
            var systemMessage = _promptBuilder.BuildSystemMessage();
            var fullUserMessage = _promptBuilder.BuildUserMessage(context, userMessage);

            return await CallApiAsync(systemMessage, fullUserMessage, AICoachRequestType.FreeChat);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送对话请求失败");
            return AICoachResponse.Fail($"请求失败: {ex.Message}", AICoachRequestType.FreeChat);
        }
    }

    /// <inheritdoc />
    public async Task<bool> IsAvailableAsync()
    {
        if (string.IsNullOrEmpty(_settings.ApiKey))
        {
            return false;
        }

        try
        {
            // 简单的可用性检查：发送一个最小请求
            var request = new ChatCompletionRequest
            {
                Model = _settings.Model,
                Messages = new List<ChatMessage>
                {
                    new() { Role = "user", Content = "hi" }
                },
                MaxTokens = 5
            };

            var response = await _httpClient.PostAsJsonAsync("chat/completions", request);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// 调用 API
    /// </summary>
    private async Task<AICoachResponse> CallApiAsync(string systemMessage, string userMessage, AICoachRequestType requestType)
    {
        var startTime = DateTime.Now;

        var request = new ChatCompletionRequest
        {
            Model = _settings.Model,
            Messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemMessage },
                new() { Role = "user", Content = userMessage }
            },
            MaxTokens = _settings.MaxTokens,
            Temperature = _settings.Temperature
        };

        Log.Debug("发送 AI 请求: Model={Model}, SystemLength={SystemLength}, UserLength={UserLength}",
            request.Model, systemMessage.Length, userMessage.Length);

        var response = await _httpClient.PostAsJsonAsync("chat/completions", request);
        var latencyMs = (int)(DateTime.Now - startTime).TotalMilliseconds;

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            Log.Error("API 请求失败: StatusCode={StatusCode}, Content={Content}",
                response.StatusCode, errorContent);

            return new AICoachResponse
            {
                Success = false,
                ErrorMessage = $"API 错误 ({response.StatusCode}): {errorContent}",
                RequestType = requestType,
                LatencyMs = latencyMs
            };
        }

        var result = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>();

        if (result?.Choices == null || result.Choices.Count == 0)
        {
            return new AICoachResponse
            {
                Success = false,
                ErrorMessage = "API 返回空响应",
                RequestType = requestType,
                LatencyMs = latencyMs
            };
        }

        var content = result.Choices[0].Message?.Content ?? string.Empty;

        Log.Information("AI 响应成功: Latency={Latency}ms, ContentLength={ContentLength}",
            latencyMs, content.Length);

        return new AICoachResponse
        {
            Success = true,
            Content = content,
            RequestType = requestType,
            LatencyMs = latencyMs,
            TokenUsage = result.Usage != null ? new TokenUsage
            {
                PromptTokens = result.Usage.PromptTokens,
                CompletionTokens = result.Usage.CompletionTokens
            } : null
        };
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient.Dispose();
            _disposed = true;
        }
    }

    #region API 请求/响应模型

    private class ChatCompletionRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public List<ChatMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 1000;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }

    private class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    private class ChatCompletionResponse
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("choices")]
        public List<Choice>? Choices { get; set; }

        [JsonPropertyName("usage")]
        public Usage? Usage { get; set; }
    }

    private class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    private class Usage
    {
        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }

        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }
    }

    #endregion
}

/// <summary>
/// AI Coach 服务设置
/// </summary>
public class AICoachSettings
{
    /// <summary>
    /// API 基础 URL
    /// </summary>
    public string ApiBaseUrl { get; set; } = "https://api.openai.com/v1/";

    /// <summary>
    /// API Key
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// 模型名称
    /// </summary>
    public string Model { get; set; } = "gpt-4o-mini";

    /// <summary>
    /// 最大 Token 数
    /// </summary>
    public int MaxTokens { get; set; } = 1000;

    /// <summary>
    /// 温度参数
    /// </summary>
    public double Temperature { get; set; } = 0.7;

    /// <summary>
    /// 超时时间（秒）
    /// </summary>
    public int TimeoutSeconds { get; set; } = 60;
}
