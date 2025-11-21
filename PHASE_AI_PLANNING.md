# RecordTime AI 模块开发规划

**规划日期**: 2025-11-21
**当前状态**: Phase AI-1 已完成（AI 分析基础功能）

---

## 更新日志

### 2025-11-21 - Phase AI-1 完成

**已实现功能**:
- ✅ AI 服务抽象层接口 (`IAIAnalysisService.cs`)
- ✅ OpenAI 云端服务实现 (`OpenAIService.cs`)
- ✅ AI 数据模型 (`AIAnalysisInput.cs`, `AIAnalysisResult.cs`)
- ✅ 报告数据构建器 (`ReportDataBuilder.cs`)
- ✅ HTML 报告 AI 分析结果嵌入
- ✅ ReportView 添加 AI 配置面板
- ✅ 支持 API Key、模型、Base URL 配置
- ✅ AI 连接测试功能

**新增文件**:
- `src/RecordTime.Core/Models/AI/AIAnalysisInput.cs`
- `src/RecordTime.Core/Models/AI/AIAnalysisResult.cs`
- `src/RecordTime.Core/Services/AI/IAIAnalysisService.cs`
- `src/RecordTime.Core/Services/AI/OpenAIService.cs`
- `src/RecordTime.Data/Reports/ReportDataBuilder.cs`

**修改文件**:
- `src/RecordTime.Core/RecordTime.Core.csproj` - 添加 OpenAI SDK
- `src/RecordTime.Data/Reports/HtmlReportGenerator.cs` - AI 结果嵌入
- `src/RecordTime.Avalonia/ViewModels/ReportViewModel.cs` - AI 分析逻辑
- `src/RecordTime.Avalonia/Views/ReportView.axaml` - AI 配置 UI

---

## 核心设计理念

> **报告驱动的 AI 分析**：图表和详细数据放在生成的报告中，而非挤在应用 UI 中，保持主界面简洁。AI 基于结构化报告数据进行分析，提供个性化建议。

### 架构优势

1. **UI 简洁性**：主界面仅展示核心信息（TOP 10 应用），详细图表在报告中呈现
2. **AI 分析友好**：JSON 结构化数据便于 AI 理解和分析
3. **隐私可控**：用户可选择是否启用 AI 分析，数据脱敏后才发送
4. **灵活扩展**：支持本地/云端 AI 切换，满足不同用户需求

---

## 系统架构

```
┌─────────────────────────────────────────────────────────────┐
│                    RecordTime AI 模块                        │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌──────────────┐    ┌──────────────┐    ┌──────────────┐  │
│  │   数据采集    │───▶│  报告生成器   │───▶│   AI 分析    │  │
│  │  (SQLite)    │    │ (HTML+JSON)  │    │ (本地/云端)  │  │
│  └──────────────┘    └──────────────┘    └──────────────┘  │
│                              │                    │         │
│                              ▼                    ▼         │
│                    ┌──────────────────────────────┐        │
│                    │     增强报告 + AI 洞察         │        │
│                    │  (HTML 报告内嵌 AI 分析结果)   │        │
│                    └──────────────────────────────┘        │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

---

## 实施阶段

### Phase AI-1: 云端 AI 集成（优先）

**目标**: 快速实现 AI 分析功能，验证用户价值
**预计工作量**: 2-3 天

#### 任务清单

##### 1.1 AI 服务抽象层 (2小时)

**文件**: `src/RecordTime.Core/Services/AI/IAIAnalysisService.cs`

```csharp
public interface IAIAnalysisService
{
    /// <summary>
    /// 分析用户时间使用数据
    /// </summary>
    Task<AIAnalysisResult> AnalyzeAsync(AIAnalysisInput input, CancellationToken ct = default);

    /// <summary>
    /// 服务是否可用
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 服务名称
    /// </summary>
    string ServiceName { get; }
}
```

**数据模型**: `src/RecordTime.Core/Models/AI/`

```csharp
// AI 分析输入（脱敏数据）
public class AIAnalysisInput
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public double TotalActiveHours { get; set; }
    public Dictionary<string, double> CategoryHours { get; set; }
    public Dictionary<string, double> ActivityHours { get; set; }
    public int SessionCount { get; set; }
    public int UniqueAppCount { get; set; }
    public double AvgSessionMinutes { get; set; }
    public int PeakHour { get; set; }  // 最活跃小时 (0-23)
}

// AI 分析结果
public class AIAnalysisResult
{
    public string Summary { get; set; }           // 整体评价
    public List<string> Insights { get; set; }    // 关键洞察
    public List<string> Issues { get; set; }      // 发现的问题
    public List<Recommendation> Recommendations { get; set; }  // 改进建议
    public int ProductivityScore { get; set; }    // 效率评分 (0-100)
    public DateTime AnalyzedAt { get; set; }
    public string ModelUsed { get; set; }
}

public class Recommendation
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string Priority { get; set; }  // High, Medium, Low
}
```

##### 1.2 OpenAI 云端服务实现 (4小时)

**文件**: `src/RecordTime.Core/Services/AI/OpenAIService.cs`

**依赖**: 添加 NuGet 包 `OpenAI` 或 `Azure.AI.OpenAI`

**实现要点**:
- 使用 GPT-4o-mini (成本低、速度快)
- 支持流式响应（可选）
- 错误处理和重试机制
- API Key 从设置中读取

**Prompt 设计**:
```
你是一个专业的时间管理顾问。请分析以下用户的时间使用数据，并提供个性化建议。

## 数据概览
- 分析时段: {StartDate} 至 {EndDate}
- 总活跃时间: {TotalActiveHours} 小时
- 使用应用数: {UniqueAppCount} 个
- 会话次数: {SessionCount} 次
- 平均会话时长: {AvgSessionMinutes} 分钟
- 最活跃时段: {PeakHour}:00

## 分类使用时长
{CategoryBreakdown}

## 活动类型分布
{ActivityBreakdown}

请从以下角度分析并以 JSON 格式回复:
1. summary: 用 2-3 句话概括整体时间使用情况
2. insights: 3-5 条关键发现（如最耗时的分类、工作效率模式等）
3. issues: 发现的潜在问题（如娱乐时间过长、频繁切换应用等）
4. recommendations: 3-5 条具体可行的改进建议，每条包含 title、description、priority
5. productivityScore: 0-100 的效率评分

回复格式:
{
  "summary": "...",
  "insights": ["...", "..."],
  "issues": ["...", "..."],
  "recommendations": [
    {"title": "...", "description": "...", "priority": "High/Medium/Low"}
  ],
  "productivityScore": 75
}
```

##### 1.3 增强报告生成器 (3小时)

**文件**: `src/RecordTime.Data/Reports/EnhancedReportGenerator.cs`

**新增功能**:
1. `GenerateAnalysisData()` - 生成 AI 分析所需的 JSON 数据
2. `EmbedAIAnalysis()` - 将 AI 分析结果嵌入 HTML 报告
3. 增强图表（添加每日趋势、时间段分布）

**报告结构增强**:
```html
<!-- 原有内容 -->
<section class="summary">...</section>
<section class="charts">...</section>
<section class="app-table">...</section>

<!-- 新增: AI 分析部分 -->
<section class="ai-analysis">
    <h2>AI 智能分析</h2>
    <div class="ai-summary">
        <p class="score">效率评分: 75/100</p>
        <p class="summary">整体评价文字...</p>
    </div>
    <div class="ai-insights">
        <h3>关键洞察</h3>
        <ul>...</ul>
    </div>
    <div class="ai-recommendations">
        <h3>改进建议</h3>
        <div class="recommendation-card">...</div>
    </div>
</section>
```

##### 1.4 更新报告页面 UI (2小时)

**文件**: `src/RecordTime.Avalonia/Views/ReportView.axaml`

**新增元素**:
- [AI 分析] 按钮
- AI 分析状态显示
- API Key 配置入口（跳转设置页面）

**交互流程**:
```
1. 用户选择日期范围
2. 点击 [生成报告] → 生成 HTML 报告（不含 AI 分析）
3. 点击 [AI 分析] →
   - 检查 API Key 配置
   - 调用 AI 服务
   - 将结果嵌入报告
   - 自动刷新/重新打开报告
```

##### 1.5 设置页面增强 (2小时)

**文件**: `src/RecordTime.Avalonia/Views/SettingsView.axaml`

**新增设置项**:
```
AI 分析设置
├── 启用 AI 分析: [开关]
├── AI 服务: [下拉] OpenAI / 本地模型(即将支持)
├── API Key: [密码输入框] ********
├── 数据隐私: [下拉] 仅分类数据 / 包含应用名称
└── [测试连接] 按钮
```

---

### Phase AI-2: 报告内容增强

**目标**: 丰富报告图表，提供更详细的数据可视化
**预计工作量**: 2 天

#### 任务清单

##### 2.1 每日趋势图 (3小时)
- 7天/30天折线图
- 显示每日总使用时长
- 与上期对比（可选）

##### 2.2 时间段分布图 (3小时)
- 24小时活动热力图或柱状图
- 显示各时段的使用情况
- 突出显示高效时段

##### 2.3 活动类型详细分析 (2小时)
- 饼图展示活动类型占比
- 各类型的详细说明

##### 2.4 上期对比分析 (2小时)
- 本周 vs 上周
- 变化百分比
- 趋势箭头

---

### Phase AI-3: 本地 AI 支持

**目标**: 支持完全离线的 AI 分析
**预计工作量**: 3-5 天

#### 技术方案

**选项 A: Ollama 集成**
- 优点: 部署简单，模型管理方便
- 缺点: 需要用户安装 Ollama

**选项 B: LlamaSharp 直接集成**
- 优点: 无需额外安装
- 缺点: 模型下载管理复杂

**推荐**: 先支持 Ollama，后续考虑 LlamaSharp

#### 任务清单

##### 3.1 Ollama 服务实现 (4小时)
**文件**: `src/RecordTime.Core/Services/AI/OllamaService.cs`

##### 3.2 模型推荐和下载指引 (2小时)
- 推荐模型: llama3.1:8b, qwen2.5:7b
- 在设置页面提供安装指引

##### 3.3 自动服务切换 (2小时)
- Ollama 不可用时自动切换到 OpenAI
- 用户可手动选择偏好

---

## 数据隐私策略

### 脱敏级别

**级别 1 - 仅分类数据（默认）**
```json
{
  "categoryHours": {
    "开发工具": 18.2,
    "浏览器": 12.5,
    "视频娱乐": 6.3
  }
}
```

**级别 2 - 包含通用应用名称**
```json
{
  "categoryHours": { ... },
  "topApps": [
    {"name": "Code Editor", "hours": 10.2},
    {"name": "Browser", "hours": 8.5}
  ]
}
```

### 不发送的数据
- 具体进程名（如 `chrome.exe`）
- 窗口标题（已哈希）
- 具体 URL 或文件路径
- 时间戳精确到秒

---

## 文件结构

```
src/RecordTime.Core/
├── Models/AI/
│   ├── AIAnalysisInput.cs
│   ├── AIAnalysisResult.cs
│   └── Recommendation.cs
├── Services/AI/
│   ├── IAIAnalysisService.cs
│   ├── OpenAIService.cs
│   └── OllamaService.cs (Phase AI-3)

src/RecordTime.Data/
├── Reports/
│   ├── HtmlReportGenerator.cs (增强)
│   └── ReportDataBuilder.cs (新增)

src/RecordTime.Avalonia/
├── ViewModels/
│   └── ReportViewModel.cs (增强)
├── Views/
│   ├── ReportView.axaml (增强)
│   └── SettingsView.axaml (增强)
```

---

## NuGet 依赖

### Phase AI-1 需要添加
```xml
<!-- OpenAI SDK -->
<PackageReference Include="OpenAI" Version="2.0.0" />
<!-- 或使用 Azure SDK -->
<PackageReference Include="Azure.AI.OpenAI" Version="1.0.0-beta.17" />
```

### Phase AI-3 需要添加
```xml
<!-- Ollama 客户端 (如果直接调用 API 则不需要) -->
<!-- 或使用 LlamaSharp -->
<PackageReference Include="LLamaSharp" Version="0.12.0" />
<PackageReference Include="LLamaSharp.Backend.Cuda12" Version="0.12.0" />
```

---

## 成功标准

### Phase AI-1 完成标准
- [x] 用户可配置 OpenAI API Key
- [x] 点击 [生成报告（含 AI 分析）] 按钮能成功调用 AI
- [x] AI 分析结果正确嵌入 HTML 报告
- [x] 分析结果包含: 总结、洞察、问题、建议、评分
- [x] 数据脱敏有效，不泄露敏感信息
- [x] 错误处理完善（API 失败、网络问题等）

### Phase AI-2 完成标准
- [ ] 报告包含每日趋势图
- [ ] 报告包含时间段分布图
- [ ] 图表美观，符合 Apple 设计风格
- [ ] 与上期对比数据正确

### Phase AI-3 完成标准
- [ ] Ollama 本地分析功能可用
- [ ] 支持本地/云端切换
- [ ] 离线状态下 AI 分析正常工作

---

## 开发注意事项

### API Key 安全
- 不要将 API Key 硬编码
- 使用 Windows Credential Manager 或加密存储
- 在 UI 中使用密码框显示

### 错误处理
- API 调用超时处理 (建议 30s)
- 网络错误友好提示
- JSON 解析失败的回退方案

### 性能优化
- AI 分析异步执行，不阻塞 UI
- 显示加载状态和进度
- 缓存分析结果（同一时间范围不重复分析）

### 用户体验
- 首次使用提供引导
- API Key 配置不正确时给出明确提示
- 分析过程中显示预估时间

---

## 时间表

| 阶段 | 预计开始时间 | 预计完成时间 | 工作量 |
|------|------------|------------|-------|
| Phase AI-1 | 2025-11-21 | 2025-11-23 | 2-3 天 |
| Phase AI-2 | 2025-11-24 | 2025-11-25 | 2 天 |
| Phase AI-3 | 2025-11-26 | 2025-11-30 | 3-5 天 |

---

**文档版本**: v1.0
**最后更新**: 2025-11-21
