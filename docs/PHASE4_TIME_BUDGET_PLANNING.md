# RecordTime Phase 4: 时间预算与目标系统

**规划日期**: 2025-11-25
**分支**: `feature/time-budget-goals`
**状态**: 规划中

---

## 1. 功能概述

### 1.1 核心理念

> **从"记录时间"到"管理时间"的跨越**

RecordTime 目前能够准确记录用户的应用使用时间，但缺少帮助用户**改变行为**的能力。Phase 4 将引入"时间预算与目标系统"，让用户能够：

1. 设定时间使用目标
2. 实时追踪目标进度
3. 在偏离目标时收到提醒
4. 每日总结与改进建议

### 1.2 用户价值

| 痛点 | 解决方案 | 价值 |
|------|---------|------|
| 不知道时间花在哪里 | ✅ 已有仪表盘 | 已解决 |
| 不知道该设什么目标 | AI 智能建议目标 | 降低决策负担 |
| 设了目标但忘记执行 | 实时进度追踪 | 可视化感知 |
| 超时了才发现 | 超时提醒通知 | 及时干预 |
| 不知道如何改进 | 日末总结 + AI 建议 | 持续优化 |

---

## 2. 功能设计

### 2.1 AI 智能目标建议

#### 用户流程

```
用户点击"设置目标"
       ↓
系统分析过去 7 天数据
       ↓
生成智能建议列表：
  • 娱乐类应用：建议减少 20-30%
  • 生产力应用：建议增加 10-20%
  • 高频应用：自动识别并建议
       ↓
用户可以：
  • 一键接受全部建议
  • 逐个调整数值
  • 添加自定义目标
  • 忽略某些建议
       ↓
保存并开始追踪
```

#### 建议生成规则

```csharp
// 娱乐类（视频、游戏、社交）：建议上限 = 当前平均 × 0.75
// 生产力类（开发、办公）：建议下限 = 当前平均 × 1.15
// 通用规则：目标不超过每天 8 小时，不低于 15 分钟
```

#### UI 设计稿

```
┌─────────────────────────────────────────────────────┐
│  🎯 AI 智能目标建议                    [刷新建议]    │
├─────────────────────────────────────────────────────┤
│                                                     │
│  基于您过去 7 天的使用数据，我们建议：               │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ 📱 微信                          [删除]     │   │
│  │ 当前平均：2.3 小时/天                        │   │
│  │ 目标类型：(•) 上限  ( ) 下限                 │   │
│  │ 建议目标：[ 1.5 ] 小时/天                    │   │
│  │ [✓] 启用提醒  提醒阈值：[ 80 ]%             │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ 💻 Visual Studio Code              [删除]    │   │
│  │ 当前平均：3.8 小时/天                        │   │
│  │ 目标类型：( ) 上限  (•) 下限                 │   │
│  │ 建议目标：[ 4.0 ] 小时/天                    │   │
│  │ [✓] 启用提醒  提醒阈值：[ 80 ]%             │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  ┌─────────────────────────────────────────────┐   │
│  │ 🎬 视频娱乐（分类目标）            [删除]    │   │
│  │ 当前平均：3.1 小时/天                        │   │
│  │ 目标类型：(•) 上限  ( ) 下限                 │   │
│  │ 建议目标：[ 2.0 ] 小时/天                    │   │
│  │ [✓] 启用提醒  提醒阈值：[ 80 ]%             │   │
│  └─────────────────────────────────────────────┘   │
│                                                     │
│  [+ 添加应用目标]  [+ 添加分类目标]                  │
│                                                     │
│  ─────────────────────────────────────────────────  │
│                                                     │
│  [ 一键接受全部建议 ]        [ 保存设置 ]           │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### 2.2 实时进度追踪

#### 仪表盘显示

在现有仪表盘中添加"目标进度"区域：

```
┌─────────────────────────────────────────────────────┐
│  📊 今日目标进度                                    │
├─────────────────────────────────────────────────────┤
│                                                     │
│  微信                    1.2h / 1.5h (上限)        │
│  ████████████████░░░░░░░░░░░░░░░░░░░░  80% ⚠️      │
│                                                     │
│  VS Code                 2.5h / 4.0h (下限)        │
│  ████████████████░░░░░░░░░░░░░░░░░░░░  63%         │
│                                                     │
│  视频娱乐                0.5h / 2.0h (上限)        │
│  ████████░░░░░░░░░░░░░░░░░░░░░░░░░░░░  25% ✓       │
│                                                     │
└─────────────────────────────────────────────────────┘

图例：
  ✓ 良好（上限<80% 或 下限>80%）
  ⚠️ 警告（接近阈值）
  ❌ 超标（上限>100% 或 下限<50%且已过半天）
```

### 2.3 超时提醒通知

#### 提醒触发条件

| 目标类型 | 触发条件 | 提醒内容 |
|---------|---------|---------|
| 上限目标 | 达到 80% | "微信已使用 1.2 小时，接近今日上限 1.5 小时" |
| 上限目标 | 达到 100% | "微信已达到今日上限 1.5 小时" |
| 下限目标 | 18:00 时 <50% | "VS Code 今日仅使用 1.5 小时，距离目标还差 2.5 小时" |

#### 通知样式

使用托盘气泡通知（简单可靠，无需额外配置）：

```
┌─────────────────────────────────────────┐
│ 🎯 RecordTime                      ✕   │
├─────────────────────────────────────────┤
│                                         │
│ ⚠️ 微信使用时间提醒                     │
│                                         │
│ 已使用 1.2 小时，接近今日上限 1.5 小时   │
│                                         │
└─────────────────────────────────────────┘
```

**技术方案**：使用现有 TrayIconService 的气泡通知能力，避免 Windows Toast 的 AUMID 注册复杂性。

#### 免打扰设置

- 可设置免打扰时间段（如 22:00 - 08:00）
- 可临时暂停提醒（1 小时 / 今天剩余时间）
- 同一目标每 30 分钟最多提醒一次

### 2.4 日末总结

#### 触发时间

默认每天 18:00（可在设置中调整）

#### 总结内容

```
┌─────────────────────────────────────────────────────┐
│  📋 今日时间总结                      2025-11-25   │
├─────────────────────────────────────────────────────┤
│                                                     │
│  🎯 目标完成情况                                    │
│  ────────────────────────────────────────────────   │
│  ✓ 视频娱乐    0.8h / 2.0h 上限    完成 ✓          │
│  ⚠️ 微信       1.8h / 1.5h 上限    超出 20%        │
│  ✓ VS Code    4.2h / 4.0h 下限    完成 ✓          │
│                                                     │
│  📊 整体表现：2/3 目标达成 (67%)                    │
│                                                     │
│  💡 AI 建议                                         │
│  ────────────────────────────────────────────────   │
│  1. 微信使用超出目标，建议明天设置"专注模式"        │
│     在工作时段屏蔽社交应用提醒                      │
│                                                     │
│  2. VS Code 使用稳定，继续保持！可以考虑            │
│     将目标提升到 4.5 小时以进一步提高生产力         │
│                                                     │
│  [查看详细报告]  [调整明日目标]  [关闭]             │
│                                                     │
└─────────────────────────────────────────────────────┘
```

---

## 3. 数据模型设计

### 3.1 新增表：TimeBudget（时间预算）

```csharp
public class TimeBudget
{
    public int Id { get; set; }

    // 目标对象（二选一）
    public string? ProcessName { get; set; }      // 应用进程名（如 "WeChat"）
    public string? Category { get; set; }         // 分类名（如 "视频娱乐"）

    // 目标设置
    public BudgetType Type { get; set; }          // 上限 / 下限
    public int TargetMinutes { get; set; }        // 目标时长（分钟）

    // 提醒设置
    public bool ReminderEnabled { get; set; }     // 是否启用提醒
    public int ReminderThreshold { get; set; }    // 提醒阈值（百分比，默认 80）

    // 元数据
    public bool IsEnabled { get; set; }           // 是否启用此目标
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum BudgetType
{
    Maximum,  // 上限（不超过）
    Minimum   // 下限（至少达到）
}
```

### 3.2 新增表：DailyBudgetProgress（每日进度记录）

```csharp
public class DailyBudgetProgress
{
    public int Id { get; set; }

    public int TimeBudgetId { get; set; }
    public TimeBudget TimeBudget { get; set; }

    public DateTime Date { get; set; }            // 日期
    public int ActualMinutes { get; set; }        // 实际使用时长
    public int TargetMinutes { get; set; }        // 当日目标（快照）
    public BudgetType BudgetType { get; set; }    // 目标类型（快照）
    public int ReminderThreshold { get; set; }    // 提醒阈值（快照）
    public bool IsCompleted { get; set; }         // 是否达成

    public DateTime? LastReminderTime { get; set; } // 最后提醒时间
}
```

**索引设计**：
- `IX_DailyBudgetProgress_TimeBudgetId_Date`（唯一索引）
- `IX_DailyBudgetProgress_Date`（日期查询优化）

### 3.3 新增表：GoalSuggestion（目标建议缓存）

```csharp
public class GoalSuggestion
{
    public int Id { get; set; }

    public string ProcessName { get; set; }       // 应用名
    public string? Category { get; set; }         // 分类
    public string DisplayName { get; set; }       // 显示名称

    public double CurrentAverageMinutes { get; set; }  // 当前平均（分钟）
    public int SuggestedMinutes { get; set; }          // 建议目标
    public BudgetType SuggestedType { get; set; }      // 建议类型

    public DateTime GeneratedAt { get; set; }     // 生成时间
}
```

---

## 4. 技术实现计划

### 4.1 前置任务：修复技术问题

**问题 1**: async void 定时器回调（中风险）

位置：
- `MainWindowViewModel.cs:265-286`
- `ReportViewModel.cs:226-233`
- `SessionManager.cs:281-285`（心跳定时器）
- `SessionManager.cs:329-334`（空闲检查定时器）

修复方案：
```csharp
// 修复前
_timer = new Timer(async _ => { ... }, ...);

// 修复后
_timer = new Timer(_ =>
{
    Task.Run(async () =>
    {
        try { ... }
        catch (Exception ex) { _logger.Error(ex, "Timer callback failed"); }
    });
}, ...);
```

**问题 2**: InputMonitor 缺少 IDisposable（低风险）

位置：`InputMonitor.cs:370-373`

修复方案：实现 IDisposable 接口，添加 GC.SuppressFinalize

### 4.2 阶段 1：数据模型与基础设施（2-3 小时）

1. 创建数据模型类
   - `TimeBudget.cs`
   - `DailyBudgetProgress.cs`
   - `GoalSuggestion.cs`
   - `BudgetType.cs` 枚举

2. 更新 DbContext
   - 添加 DbSet
   - 配置索引

3. 创建数据库迁移
   ```bash
   dotnet ef migrations add AddTimeBudgetTables --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia
   ```

4. 创建 Repository
   - `ITimeBudgetRepository.cs`
   - `TimeBudgetRepository.cs`

### 4.3 阶段 2：AI 建议引擎（2-3 小时）

1. 创建建议服务
   - `IGoalSuggestionService.cs`
   - `GoalSuggestionService.cs`

2. 实现建议算法
   - 分析过去 7 天数据
   - 按分类生成建议
   - 缓存建议结果

3. 多语言支持
   - 添加相关字符串到 `ChineseStrings.cs` / `EnglishStrings.cs`

### 4.4 阶段 3：目标设置 UI（3-4 小时）

1. 创建 ViewModel
   - `TimeBudgetViewModel.cs`

2. 创建 View
   - `TimeBudgetView.axaml`

3. 集成到设置页面
   - 在 SettingsView 添加入口
   - 或创建独立的"目标"标签页

### 4.5 阶段 4：进度追踪与显示（2-3 小时）

1. 创建进度追踪服务
   - `IBudgetTrackingService.cs`
   - `BudgetTrackingService.cs`

2. **复用现有跨天会话分割逻辑**
   - 参考 `SessionRepository.cs:117-176` 的 `GetWeeklyTrendAsync` 方法
   - 该方法已实现跨天会话的正确分割（按天界切分，计算 effectiveStart/End）
   - 抽取为共享方法，供预算进度计算复用

3. 更新仪表盘
   - 添加目标进度区域
   - 实时更新进度条

4. 与 SessionManager 集成
   - 在会话结束时更新进度

### 4.6 阶段 5：提醒通知（2-3 小时）

1. 扩展 TrayIconService
   - 添加气泡通知方法 `ShowBalloonTip(title, message)`
   - 复用现有托盘图标基础设施

2. 实现提醒逻辑
   - 阈值检测
   - 免打扰控制
   - 防重复提醒

3. 集成到追踪服务

### 4.7 阶段 6：日末总结（2-3 小时）

1. 创建总结服务
   - `IDailySummaryService.cs`
   - `DailySummaryService.cs`

2. 实现总结生成
   - 目标完成情况统计
   - AI 建议生成（复用现有 AI 接口）

3. 创建总结弹窗 UI
   - `DailySummaryWindow.axaml`

4. 定时触发
   - 使用 Timer 在指定时间触发

---

## 5. 多语言支持

### 5.1 新增字符串（中文）

```csharp
// 目标设置
public string TimeBudget => "时间预算";
public string SetGoals => "设置目标";
public string AISmartSuggestion => "AI 智能建议";
public string RefreshSuggestions => "刷新建议";
public string AcceptAllSuggestions => "一键接受全部";
public string SaveSettings => "保存设置";
public string AddAppGoal => "添加应用目标";
public string AddCategoryGoal => "添加分类目标";

// 目标类型
public string Maximum => "上限";
public string Minimum => "下限";
public string CurrentAverage => "当前平均";
public string SuggestedGoal => "建议目标";
public string HoursPerDay => "小时/天";

// 提醒设置
public string EnableReminder => "启用提醒";
public string ReminderThreshold => "提醒阈值";

// 进度追踪
public string TodayGoalProgress => "今日目标进度";
public string GoalCompleted => "已完成";
public string GoalExceeded => "已超出";
public string GoalInProgress => "进行中";

// 通知
public string UsageReminder => "使用时间提醒";
public string ApproachingLimit => "接近今日上限";
public string ReachedLimit => "已达到今日上限";
public string PauseReminder => "暂停提醒";
public string ViewDetails => "查看详情";

// 日末总结
public string DailySummary => "今日时间总结";
public string GoalCompletionStatus => "目标完成情况";
public string OverallPerformance => "整体表现";
public string AISuggestions => "AI 建议";
public string ViewDetailedReport => "查看详细报告";
public string AdjustTomorrowGoals => "调整明日目标";
```

### 5.2 新增字符串（英文）

```csharp
public string TimeBudget => "Time Budget";
public string SetGoals => "Set Goals";
public string AISmartSuggestion => "AI Smart Suggestions";
// ... 对应英文翻译
```

---

## 6. 测试计划

### 6.1 手动测试用例

| 测试场景 | 步骤 | 预期结果 |
|---------|------|---------|
| 生成建议 | 点击"刷新建议" | 显示基于历史数据的目标建议 |
| 保存目标 | 调整目标值，点击保存 | 目标保存到数据库 |
| 进度显示 | 使用应用一段时间 | 仪表盘进度条更新 |
| 提醒触发 | 使用达到阈值 | 收到 Windows 通知 |
| 日末总结 | 等待触发时间 | 弹出总结窗口 |

### 6.2 边界条件测试

- 无历史数据时的建议生成
- 目标值为 0 或极大值
- 跨天使用时的进度重置
- 免打扰时段的提醒抑制

---

## 7. 风险与缓解

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| Windows 通知 API 兼容性 | 部分系统可能不支持 | 提供降级方案（托盘气泡） |
| 性能影响 | 频繁检查可能影响性能 | 使用节流机制，每 30 秒检查一次 |
| 用户疲劳 | 过多提醒导致用户忽略 | 智能防重复，每目标每 30 分钟最多一次 |

---

## 8. 后续迭代方向

完成 Phase 4 后，可以考虑：

1. **专注模式** - 临时屏蔽特定应用的监控/提醒
2. **周报/月报** - 自动生成周期性总结
3. **目标模板** - 预设的目标配置（如"工作模式"、"学习模式"）
4. **数据导出** - 导出目标完成历史到 CSV/Excel
5. **桌面小组件** - 快速查看目标进度的悬浮窗

---

## 9. 参考资料

- [Windows Toast Notifications](https://docs.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-notifications-overview)
- [Avalonia Notification](https://docs.avaloniaui.net/docs/concepts/services/notifications)
- [LiveCharts2 Progress Bar](https://lvcharts.com/docs/avalonia/2.0.0-rc2/gallery)

---

**文档版本**: 1.1
**最后更新**: 2025-11-25
**作者**: Claude + Codex

---

## 附录：Codex 审查修订记录

### 审查日期：2025-11-25

**发现的问题及处理**：

| 问题 | Codex 建议 | 处理结果 |
|------|-----------|---------|
| SessionManager async void | 需要修复心跳/空闲检查定时器 | ✅ 已添加到修复列表 |
| 数据模型唯一索引 | 添加 (ProcessName, Type) 唯一约束 | ❌ 暂不添加，保留多目标灵活性 |
| DailyBudgetProgress 快照 | 需要保存 BudgetType 和 ReminderThreshold | ✅ 已添加字段 |
| 跨天会话分割 | 复用 SessionRepository 现有逻辑 | ✅ 已添加到阶段 4 |
| Windows Toast 复杂性 | 需要 AUMID 注册 | ✅ 改用托盘气泡通知 |
| StringResources 接口 | 需要更新接口定义 | ✅ 在实现时处理 |
