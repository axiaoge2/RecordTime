# RecordTime Phase 3 开发规划

**规划日期**: 2025-11-16
**当前状态**: Phase 2 已完成（托盘图标、开机自启动）

---

## 📊 核心问题诊断

通过深度分析，发现当前应用存在的**关键问题**：

> **RecordTime 能够"记录"，但不能"分析"和"洞察"**

### 当前功能完成情况
✅ Phase 1: 核心监控（WindowMonitor, SessionManager, ActivityDetector）
✅ Phase 2.1: UI 改进（图标提取和显示）
✅ Phase 2.5: 系统托盘（托盘图标、最小化到托盘、开机自启动）

### 功能缺口
用户看到的只是原始数据（应用名称 + 时长），但缺少：
- ❌ 数据趋势可视化
- ❌ 智能洞察和建议
- ❌ 对比分析功能
- ❌ 个性化配置能力

**影响**: 用户无法从数据中获得价值，可能在试用后放弃使用。

---

## 🎯 三阶段渐进式实施计划

### **Phase 3.1: 快速价值提升** ⭐ 优先级最高
**目标**: 立即改善用户体验，展示数据价值
**预计工作量**: 3个工作日（21小时）
**交付价值**: 从"只有数据"到"有洞察"的质的飞跃

#### 核心任务清单

##### 3.1.1 今日总结卡片 （预计 4 小时）
**功能描述**:
- 显示今日总使用时长（格式化为"X 小时 Y 分钟"）
- TOP 3 最常用应用（带应用图标）
- 活动类型简要分布（视频/游戏/工作/被动/空闲）
- 使用 Apple 风格的卡片设计

**技术实现**:
```csharp
// 后端 - RecordTime.Data
public class TodaySummary
{
    public TimeSpan TotalDuration { get; set; }
    public List<AppUsageInfo> TopApps { get; set; }
    public Dictionary<ActivityType, TimeSpan> ActivityDistribution { get; set; }
}

// SessionRepository 新增方法
public TodaySummary GetTodaySummary(DateTime date)
```

**UI 位置**: 主窗口顶部或新增"概览"标签页

---

##### 3.1.2 改进应用列表展示 （预计 3 小时）
**功能描述**:
- 添加使用时长百分比（相对于今日总时长）
- 添加迷你柱状图可视化（类似进度条）
- 支持多种排序方式：
  - 按时长降序（默认）
  - 按应用名称
  - 按分类
- 改进视觉设计（增加间距、对齐）

**技术实现**:
- 修改 `AppStatsViewModel.cs`
- 修改 `AppStatsView.axaml`
- 添加百分比计算逻辑

**示例 UI**:
```
应用名称           时长      占比    [进度条]
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Visual Studio Code  2h 30m   35%    ██████████░░░░░░░░░░
Chrome             1h 45m   24%    ████████░░░░░░░░░░░░
WeChat             1h 20m   18%    ██████░░░░░░░░░░░░░░
```

---

##### 3.1.3 基础设置页面 （预计 6 小时）
**功能描述**:
- **应用分类管理**:
  - 查看当前分类和规则
  - 添加自定义分类
  - 编辑/删除分类
  - 为应用手动指定分类
- **监控控制**:
  - 暂停/恢复监控开关
  - 显示当前监控状态
- **基础偏好设置**:
  - 轮询间隔调整（默认 500ms）
  - 空闲检测时长（默认 5 分钟）

**技术实现**:
```csharp
// 新增文件
- ViewModels/SettingsViewModel.cs
- Views/SettingsView.axaml
- Views/SettingsView.axaml.cs

// 数据模型扩展
public class AppCategory
{
    public string Name { get; set; }
    public string Icon { get; set; }
    public List<string> Rules { get; set; } // 进程名匹配规则
}
```

**UI 布局**:
- 左侧导航：分类管理 | 监控设置 | 高级选项
- 右侧内容区域

---

##### 3.1.4 七天趋势折线图 （预计 5 小时）
**功能描述**:
- 显示过去 7 天每日总使用时长
- 使用 LiveCharts 绘制折线图
- 支持鼠标悬停查看详细数据
- X 轴：日期，Y 轴：时长（小时）

**技术实现**:
```csharp
// 后端查询
public class DailyUsage
{
    public DateTime Date { get; set; }
    public TimeSpan TotalDuration { get; set; }
}

public List<DailyUsage> GetWeeklyTrend(DateTime endDate)
{
    var startDate = endDate.AddDays(-6);
    // SQL 查询聚合每日数据
}

// ViewModel
public class TrendsViewModel : ObservableObject
{
    public ISeries[] Series { get; set; }
    public Axis[] XAxes { get; set; }
    public Axis[] YAxes { get; set; }
}
```

**LiveCharts 配置**:
- 使用 LineSeries
- 启用动画效果
- Apple 风格配色

---

##### 3.1.5 活动类型分布饼图 （预计 3 小时）
**功能描述**:
- 可视化今日活动类型占比
- 活动类型：视频娱乐、游戏、主动输入、被动浏览、空闲
- 使用不同颜色区分
- 显示百分比和时长

**技术实现**:
```csharp
// 后端查询
public Dictionary<ActivityType, TimeSpan> GetActivityDistribution(DateTime date)

// ViewModel
public ISeries[] ActivitySeries { get; set; }
```

**配色方案**:
- 视频娱乐: #FF3B30 (红色)
- 游戏: #FF9500 (橙色)
- 主动输入: #30D158 (绿色)
- 被动浏览: #0071E3 (蓝色)
- 空闲: #8E8E93 (灰色)

---

### **Phase 3.2: 深度分析功能**
**目标**: 提供专业级数据分析能力
**预计工作量**: 2-3 周
**开始时间**: Phase 3.1 完成后

#### 优先级 P0（必做）

**3.2.1 自定义日期范围筛选**
- 日期选择器控件
- 支持选择任意日期范围
- 实时更新所有图表和统计

**3.2.2 周/月对比分析**
- 本周 vs 上周
- 本月 vs 上月
- 并排对比图表
- 变化百分比显示

**3.2.3 智能洞察生成**
- 算法分析数据变化
- 生成自然语言洞察：
  - "今天比昨天多用了 2 小时开发工具"
  - "本周视频娱乐时间增加了 30%"
  - "最高效的时间段是上午 9-11 点"
- 卡片式展示，支持点击查看详情

#### 优先级 P1（应做）

**3.2.4 数据导出**
- CSV 格式导出
- Excel 格式导出（使用 EPPlus 或 ClosedXML）
- 选择导出范围（日期、应用、分类）

**3.2.5 详细报告生成**
- PDF 格式周报/月报
- 包含图表、统计、洞察
- 支持自定义报告模板

---

### **Phase 3.3: 行为干预功能**
**目标**: 帮助用户改变行为习惯
**预计工作量**: 2 周
**开始时间**: Phase 3.2 完成后

#### 核心功能

**3.3.1 使用时长目标设定**
- 为应用或分类设置每日使用目标
- 目标类型：
  - 最大使用时长（限制）
  - 最小使用时长（激励）
- 目标管理 UI

**3.3.2 超时提醒通知**
- 到达目标时弹出 Windows 通知
- 支持 Snooze 功能
- 通知历史记录

**3.3.3 专注模式**
- 临时暂停特定应用/分类的监控
- 使用场景：工作时不追踪娱乐应用
- 定时自动恢复

**3.3.4 每日/每周总结推送**
- 定时生成使用报告
- 通过通知推送
- 支持自定义推送时间

---

## 🚀 Phase 3.1 详细实施方案

### 技术架构设计

```
RecordTime.Data (数据层)
├── Repositories/
│   └── SessionRepository.cs (扩展)
│       ├── GetTodaySummary(DateTime date)
│       ├── GetWeeklyTrend(DateTime endDate)
│       ├── GetActivityDistribution(DateTime date)
│       └── GetAppUsagePercentages(DateTime date)
│
RecordTime.Avalonia (UI层)
├── ViewModels/
│   ├── DashboardViewModel.cs (新增)
│   ├── TrendsViewModel.cs (新增)
│   ├── SettingsViewModel.cs (新增)
│   └── AppStatsViewModel.cs (改进)
│
├── Views/
│   ├── DashboardView.axaml (新增 - 总结卡片)
│   ├── TrendsView.axaml (新增 - 图表展示)
│   ├── SettingsView.axaml (新增 - 设置页面)
│   └── AppStatsView.axaml (改进 - 增强列表)
│
├── Models/
│   ├── TodaySummary.cs (新增)
│   ├── DailyUsage.cs (新增)
│   └── AppCategory.cs (新增)
│
└── MainWindow.axaml (扩展)
    └── 添加标签页导航
        ├── 概览 (DashboardView)
        ├── 趋势 (TrendsView)
        ├── 统计 (AppStatsView)
        └── 设置 (SettingsView)
```

### 实施顺序建议

**第一天**:
1. ✅ 后端：添加 `GetTodaySummary()` 方法
2. ✅ 创建 `DashboardViewModel` 和 `DashboardView`
3. ✅ 实现今日总结卡片 UI
4. ✅ 改进 `AppStatsView` 列表展示

**第二天**:
5. ✅ 后端：添加分类管理数据模型
6. ✅ 创建 `SettingsViewModel` 和 `SettingsView`
7. ✅ 实现基础设置页面 UI
8. ✅ 实现监控控制功能

**第三天**:
9. ✅ 后端：添加 `GetWeeklyTrend()` 方法
10. ✅ 创建 `TrendsViewModel` 和 `TrendsView`
11. ✅ 使用 LiveCharts 实现 7 天趋势图
12. ✅ 使用 LiveCharts 实现活动类型饼图
13. ✅ 测试和优化

---

## 💡 为什么这个规划是最优的？

### ✅ 用户价值优先
- Phase 3.1 的每个功能都能立即让用户感受到价值
- 从"只有数据"到"有洞察"的质的飞跃
- 解决了用户留存的关键问题

### ✅ 渐进式交付
- 不是一次性开发所有功能，避免过度设计
- 每个阶段都有独立的价值，可以快速迭代
- 根据用户反馈调整后续计划

### ✅ 技术可行性高
- LiveCharts 已集成（`App.axaml.cs:29`）
- MVVM 架构已完善
- SQLite + EF Core 数据层稳定
- 只需要添加新的 View 和 ViewModel

### ✅ 工作量可控
- Phase 3.1 只需 3 个工作日
- 快速见效，避免用户流失
- 可以边开发边获得反馈

### ✅ 符合敏捷开发原则
- MVP 思维：先做最小可行产品
- 快速迭代：每个功能独立，可快速上线
- 用户导向：每个功能都解决真实痛点

---

## 📋 开发检查清单

### Phase 3.1 开发前准备
- [ ] 确认 LiveCharts 依赖已正确安装
- [ ] 了解当前数据库 Schema
- [ ] 熟悉 MVVM 模式和 CommunityToolkit.Mvvm
- [ ] 准备 UI 设计稿或参考（Apple 风格）

### 每个任务的开发流程
1. **后端**：
   - [ ] 在 `SessionRepository` 添加数据查询方法
   - [ ] 创建必要的 DTO/Model 类
   - [ ] 编写单元测试（可选但推荐）

2. **ViewModel**：
   - [ ] 创建 ViewModel 类
   - [ ] 实现数据绑定属性
   - [ ] 实现必要的 Command

3. **View**：
   - [ ] 创建 XAML 文件
   - [ ] 实现 UI 布局
   - [ ] 绑定 ViewModel
   - [ ] 测试数据展示

4. **集成**：
   - [ ] 更新 MainWindow 导航
   - [ ] 测试功能完整性
   - [ ] 优化性能
   - [ ] 代码审查

---

## 🔧 技术参考

### LiveCharts 使用示例

**折线图**:
```csharp
public ISeries[] Series { get; set; } = new ISeries[]
{
    new LineSeries<DailyUsage>
    {
        Values = weeklyData,
        Mapping = (data, point) =>
        {
            point.PrimaryValue = data.TotalDuration.TotalHours;
            point.SecondaryValue = point.Context.Index;
        }
    }
};
```

**饼图**:
```csharp
public ISeries[] Series { get; set; } = new ISeries[]
{
    new PieSeries<double> { Values = new double[] { 2.5 }, Name = "视频娱乐" },
    new PieSeries<double> { Values = new double[] { 1.8 }, Name = "游戏" },
    new PieSeries<double> { Values = new double[] { 4.2 }, Name = "主动输入" },
};
```

### SQL 查询示例

**今日总结**:
```sql
SELECT
    ProcessName,
    SUM(CAST((julianday(EndTime) - julianday(StartTime)) * 24 * 60 * 60 AS INTEGER)) as TotalSeconds,
    ActivityType
FROM AppSessions
WHERE DATE(StartTime) = DATE('now', 'localtime')
GROUP BY ProcessName, ActivityType
ORDER BY TotalSeconds DESC;
```

**7 天趋势**:
```sql
SELECT
    DATE(StartTime) as Date,
    SUM(CAST((julianday(EndTime) - julianday(StartTime)) * 24 * 60 * 60 AS INTEGER)) as TotalSeconds
FROM AppSessions
WHERE DATE(StartTime) >= DATE('now', '-6 days', 'localtime')
GROUP BY DATE(StartTime)
ORDER BY Date;
```

---

## 📝 注意事项

### 隐私保护
- 窗口标题已经哈希化处理（`SessionManager.HashWindowTitle()`）
- 数据只存储在本地 SQLite
- 不上传任何数据到云端

### 性能优化
- 大数据量查询需要添加索引
- 图表数据应该缓存，避免频繁查询
- UI 更新使用节流（Throttle）机制

### 用户体验
- 所有图表使用动画效果
- 加载状态显示（Loading Spinner）
- 错误处理和提示
- Apple 风格设计一致性

---

## 🎯 成功标准

### Phase 3.1 完成标准
- [ ] 用户打开应用能立即看到今日总结
- [ ] 应用列表展示更直观（有百分比和进度条）
- [ ] 用户能够自定义应用分类
- [ ] 7 天趋势图正确显示历史数据
- [ ] 活动类型饼图准确反映时间分配
- [ ] 所有功能运行流畅，无明显卡顿
- [ ] UI 设计符合 Apple 风格规范

---

## 📅 后续规划时间表

| 阶段 | 预计开始时间 | 预计完成时间 | 工作量 |
|------|------------|------------|-------|
| Phase 3.1 | 2025-11-17 | 2025-11-19 | 3 天 |
| Phase 3.2 | 2025-11-20 | 2025-12-10 | 3 周 |
| Phase 3.3 | 2025-12-11 | 2025-12-24 | 2 周 |

---

## 🤝 开发协作

如有问题或需要讨论，请：
1. 查看本文档
2. 参考 `CLAUDE.md` 了解项目架构
3. 检查代码中的注释和文档

**祝开发顺利！** 🚀
