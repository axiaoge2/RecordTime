# AI分析模块集成方案

## 1. 功能概述

AI分析模块是RecordTime的特色功能,通过大语言模型分析用户的时间使用模式,提供个性化的时间管理建议。

## 2. 设计原则

### 2.1 隐私优先
- **本地优先**: 优先使用本地模型
- **用户授权**: 云端分析需明确授权
- **数据脱敏**: 仅传输聚合统计数据
- **透明展示**: 明确告知传输的数据范围

### 2.2 双模式支持

```
┌─────────────────────────────────────┐
│         AI Analysis Module          │
├─────────────────────────────────────┤
│                                     │
│  ┌──────────────┐  ┌─────────────┐ │
│  │  Local Model │  │  Cloud API  │ │
│  │  (默认)      │  │  (可选)     │ │
│  │              │  │             │ │
│  │ Llama 3.1 8B │  │ OpenAI API  │ │
│  │ Phi-3-mini   │  │ Azure AI    │ │
│  └──────────────┘  └─────────────┘ │
│                                     │
└─────────────────────────────────────┘
```

## 3. 数据准备

### 3.1 聚合数据模型

```csharp
public class AIAnalysisInput
{
    public DateTime AnalysisDate { get; set; }
    public TimeSpan TotalActiveTime { get; set; }

    // 按分类聚合
    public Dictionary<string, TimeSpan> CategoryUsage { get; set; }
    // 例如: { "视频娱乐": "3h15m", "开发工具": "2h40m" }

    // 按活动类型聚合
    public Dictionary<string, TimeSpan> ActivityUsage { get; set; }
    // 例如: { "Video": "3h15m", "ActiveTyping": "4h20m" }

    // 时间分布模式
    public Dictionary<int, int> HourlyDistribution { get; set; }
    // 例如: { 9: 60, 10: 120, ... } (分钟数)

    // 统计指标
    public int SessionCount { get; set; }
    public TimeSpan LongestFocusSession { get; set; }
    public int AppSwitchCount { get; set; }

    // 不包含具体进程名、窗口标题等敏感信息
}
```

### 3.2 数据脱敏流程

```csharp
class AIDataPreparation
{
    public AIAnalysisInput PrepareData(DateTime date)
    {
        // 1. 查询原始Session数据
        var sessions = await _repository.GetSessionsByDate(date);

        // 2. 聚合统计
        var categoryUsage = sessions
            .GroupBy(s => s.Category ?? "未分类")
            .ToDictionary(
                g => g.Key,
                g => TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds))
            );

        // 3. 时间分布
        var hourlyDist = sessions
            .SelectMany(s => GetHourRange(s.StartTime, s.EndTime))
            .GroupBy(h => h)
            .ToDictionary(g => g.Key, g => g.Count());

        // 4. 构建聚合数据(不包含敏感信息)
        return new AIAnalysisInput
        {
            AnalysisDate = date,
            TotalActiveTime = TimeSpan.FromSeconds(sessions.Sum(s => s.DurationSeconds)),
            CategoryUsage = categoryUsage,
            HourlyDistribution = hourlyDist,
            SessionCount = sessions.Count
        };
    }
}
```

## 4. 本地模型集成

### 4.1 技术方案: llama.cpp

```csharp
推荐模型:
- Llama 3.1 8B GGUF Q4_K_M (4.5GB)
- Phi-3-mini-4k (2.4GB)

集成库:
- LLamaSharp (C# binding for llama.cpp)
```

### 4.2 实现代码

```csharp
using LLama;
using LLama.Common;

public class LocalAIService : IAIAnalysisService
{
    private LLamaWeights? _model;
    private LLamaContext? _context;

    public async Task InitializeAsync()
    {
        var modelPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime", "Models", "llama-3.1-8b-q4.gguf"
        );

        var parameters = new ModelParams(modelPath)
        {
            ContextSize = 2048,
            GpuLayerCount = 20  // GPU加速
        };

        _model = LLamaWeights.LoadFromFile(parameters);
        _context = _model.CreateContext(parameters);
    }

    public async Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisInput input)
    {
        var prompt = BuildPrompt(input);

        var executor = new InteractiveExecutor(_context);
        var result = await executor.InferAsync(prompt);

        return ParseResponse(result);
    }

    private string BuildPrompt(AIAnalysisInput input)
    {
        return $@"
你是一个时间管理专家。请分析以下用户的时间使用数据,并提供建议:

**日期**: {input.AnalysisDate:yyyy-MM-dd}
**总活跃时间**: {input.TotalActiveTime}

**分类使用时长**:
{string.Join("\n", input.CategoryUsage.Select(kv => $"- {kv.Key}: {kv.Value}"))}

**活动类型分布**:
{string.Join("\n", input.ActivityUsage.Select(kv => $"- {kv.Key}: {kv.Value}"))}

请从以下角度提供分析:
1. 时间分配是否合理?
2. 发现的效率问题
3. 具体改进建议(3-5条)

以JSON格式回复:
{{
  ""summary"": ""整体评价"",
  ""issues"": [""问题1"", ""问题2""],
  ""recommendations"": [""建议1"", ""建议2"", ""建议3""]
}}
";
    }
}
```

## 5. 云端API集成

### 5.1 OpenAI API方案

```csharp
using OpenAI;

public class CloudAIService : IAIAnalysisService
{
    private OpenAIClient? _client;

    public void Initialize(string apiKey)
    {
        _client = new OpenAIClient(apiKey);
    }

    public async Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisInput input)
    {
        var prompt = BuildPrompt(input);

        var response = await _client.ChatCompletions.CreateAsync(
            new ChatCompletionRequest
            {
                Model = "gpt-4o-mini",
                Messages = new[]
                {
                    new ChatMessage("system", "你是一个时间管理专家"),
                    new ChatMessage("user", prompt)
                },
                Temperature = 0.7,
                MaxTokens = 1000
            }
        );

        return ParseResponse(response.Choices[0].Message.Content);
    }
}
```

### 5.2 隐私保护措施

```csharp
class CloudAIService
{
    public async Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisInput input)
    {
        // 1. 用户明确授权检查
        if (!await _settings.GetAsync<bool>("AICloudAnalysisEnabled"))
        {
            throw new UnauthorizedAccessException("未授权云端分析");
        }

        // 2. 二次脱敏确认
        ValidateNoSensitiveData(input);

        // 3. 记录传输日志
        await _logger.LogAsync($"AI分析数据传输: {input.AnalysisDate}");

        // 4. 执行分析
        return await CallAPIAsync(input);
    }

    private void ValidateNoSensitiveData(AIAnalysisInput input)
    {
        // 确保不包含进程名、窗口标题等
        var json = JsonSerializer.Serialize(input);

        if (json.Contains("chrome.exe") || json.Contains("http://"))
        {
            throw new InvalidOperationException("检测到敏感信息");
        }
    }
}
```

## 6. 分析结果模型

```csharp
public class AIAnalysisResult
{
    /// <summary>
    /// 整体评价摘要
    /// </summary>
    public string Summary { get; set; }

    /// <summary>
    /// 发现的问题
    /// </summary>
    public List<string> Issues { get; set; }

    /// <summary>
    /// 改进建议
    /// </summary>
    public List<Recommendation> Recommendations { get; set; }

    /// <summary>
    /// 时间使用评分 (0-100)
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// 分析时间
    /// </summary>
    public DateTime AnalyzedAt { get; set; }

    /// <summary>
    /// 使用的模型
    /// </summary>
    public string ModelUsed { get; set; }
}

public class Recommendation
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }  // High, Medium, Low
}
```

## 7. UI展示设计

### 7.1 分析页面布局

```xaml
<StackPanel Spacing="16">
    <!-- 分析选项 -->
    <Card>
        <RadioButtons Header="分析模式">
            <RadioButton Content="本地分析 (推荐)" IsChecked="True"/>
            <RadioButton Content="云端分析 (需要API Key)"/>
        </RadioButtons>

        <Button Content="开始分析" Click="OnAnalyze"/>
    </Card>

    <!-- 分析结果 -->
    <Card>
        <TextBlock Text="{Binding Summary}" Style="Subtitle"/>

        <ItemsRepeater ItemsSource="{Binding Recommendations}">
            <DataTemplate>
                <Card Padding="12">
                    <StackPanel>
                        <TextBlock Text="{Binding Title}" FontWeight="Bold"/>
                        <TextBlock Text="{Binding Description}"/>
                    </StackPanel>
                </Card>
            </DataTemplate>
        </ItemsRepeater>
    </Card>
</StackPanel>
```

## 8. 配置管理

### 8.1 设置页面

```csharp
public class AISettings
{
    // 基础设置
    public bool EnableAIAnalysis { get; set; } = true;
    public AIMode PreferredMode { get; set; } = AIMode.Local;

    // 本地模型设置
    public string LocalModelPath { get; set; }
    public bool UseGPU { get; set; } = true;

    // 云端API设置
    public string? OpenAIApiKey { get; set; }
    public string? AzureEndpoint { get; set; }
    public bool AllowCloudAnalysis { get; set; } = false;

    // 隐私设置
    public bool LogAnalysisRequests { get; set; } = true;
}

public enum AIMode
{
    Local,      // 本地模型
    Cloud,      // 云端API
    Hybrid      // 混合:本地失败时使用云端
}
```

## 9. 成本估算

### 9.1 本地模型
- **初始下载**: 4.5GB (一次性)
- **存储空间**: 5GB
- **运行内存**: 8GB RAM
- **GPU**: 可选,显著加速
- **费用**: 免费

### 9.2 云端API (OpenAI)
- **模型**: GPT-4o-mini
- **每次分析Token**: ~500 input + 300 output
- **成本**: ~$0.0008/次分析
- **月度成本** (每日1次): ~$0.024/月

## 10. 实现优先级

### Phase 4.1 - 基础实现
1. ✅ 数据脱敏模块
2. ✅ AIAnalysisInput模型
3. ⬜ 云端API集成 (OpenAI)
4. ⬜ 基础UI页面

### Phase 4.2 - 本地模型
5. ⬜ llama.cpp集成
6. ⬜ 模型下载器
7. ⬜ GPU加速支持

### Phase 4.3 - 高级功能
8. ⬜ 历史趋势分析
9. ⬜ 自定义分析提示词
10. ⬜ 分析报告导出

---

**建议**: 先实现云端API版本(简单),再逐步完善本地模型支持。
