# RecordTime 架构设计文档

## 1. 整体架构

RecordTime 采用 **三层架构** + **MVVM 模式** 设计：

```
┌─────────────────────────────────────────────────────────┐
│              Presentation Layer (UI)                    │
│         Avalonia UI 11.x + MVVM + Data Binding         │
│  ┌─────────────┐  ┌──────────────┐  ┌───────────────┐  │
│  │   Views     │  │  ViewModels  │  │   Resources   │  │
│  └─────────────┘  └──────────────┘  └───────────────┘  │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│              Business Logic Layer (Core)                │
│           服务层 + 业务规则 + 领域模型                    │
│  ┌─────────────────┐  ┌────────────────────────────┐   │
│  │ SessionManager  │  │ WindowMonitor              │   │
│  │ ActivityDetector│  │ InputMonitor / MediaDetector│  │
│  └─────────────────┘  └────────────────────────────┘   │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│              Data Access Layer (Data)                   │
│           EF Core + Repository + 数据服务                │
│  ┌──────────────────┐  ┌────────────────────────────┐  │
│  │ RecordTimeDb     │  │ SessionRepository          │  │
│  │ Context          │  │ TimeBudgetRepository       │  │
│  └──────────────────┘  └────────────────────────────┘  │
└────────────────────────┬────────────────────────────────┘
                         │
┌────────────────────────▼────────────────────────────────┐
│                   SQLite Database                       │
│             %LOCALAPPDATA%\RecordTime\                  │
└─────────────────────────────────────────────────────────┘
```

## 2. 项目结构

```
RecordTime/
├── src/
│   ├── RecordTime.Avalonia/        # UI 层
│   │   ├── Views/                  # XAML 视图
│   │   ├── ViewModels/             # MVVM 视图模型
│   │   ├── Services/               # UI 服务
│   │   │   ├── AppDataService.cs       # 数据聚合
│   │   │   ├── TrayIconService.cs      # 系统托盘
│   │   │   ├── BudgetTrackingService.cs # 预算追踪
│   │   │   ├── NotificationService.cs   # 通知服务
│   │   │   ├── GoalSuggestionEngine.cs  # AI 建议
│   │   │   └── DailySummaryService.cs   # 日末总结
│   │   └── Resources/Strings/      # 多语言资源
│   │
│   ├── RecordTime.Core/            # 核心业务层
│   │   ├── Models/                 # 领域模型
│   │   │   ├── AppSession.cs
│   │   │   ├── ActivityType.cs
│   │   │   ├── TimeBudget.cs
│   │   │   └── DailyBudgetProgress.cs
│   │   └── Services/               # 监控服务
│   │       ├── SessionManager.cs       # 会话管理器
│   │       ├── WindowMonitor.cs        # 窗口监控
│   │       ├── InputMonitor.cs         # 输入监控
│   │       ├── MediaDetector.cs        # 媒体检测
│   │       └── ActivityDetector.cs     # 活动分类
│   │
│   └── RecordTime.Data/            # 数据访问层
│       ├── RecordTimeDbContext.cs  # EF Core DbContext
│       ├── Repositories/           # 仓储实现
│       ├── Reports/                # 报告生成
│       └── Migrations/             # 数据库迁移
│
├── tools/                          # 工具集
└── docs/                           # 文档
```

## 3. 核心组件设计

### 3.1 SessionManager（会话管理器）

**文件**: `src/RecordTime.Core/Services/SessionManager.cs`

核心协调器，整合所有监控服务：

```
WindowMonitor ──┐
InputMonitor  ──┼──> SessionManager ──> SessionRepository
MediaDetector ──┘         │
                          ▼
                  ActivityDetector
```

**职责**：
- 订阅 `WindowMonitor.WindowFocusChanged` 事件
- 收集系统状态（输入、媒体、全屏）
- 调用 `ActivityDetector` 分类活动类型
- 创建/结束 `AppSession` 并持久化
- 实现 30 秒心跳机制防止数据丢失
- 2 分钟间隔空闲检查

**关键方法**：
```csharp
public void Start()           // 启动监控
public void Stop()            // 停止监控
private void OnWindowFocusChanged()  // 窗口切换处理
private void HeartbeatTimer_Elapsed() // 心跳更新
```

### 3.2 WindowMonitor（窗口监控器）

**文件**: `src/RecordTime.Core/Services/WindowMonitor.cs`

使用 Win32 API 轮询前台窗口：

| 参数 | 值 |
|------|-----|
| 轮询间隔 | 500ms |
| Win32 API | GetForegroundWindow, GetWindowText, GetWindowThreadProcessId |

**事件**：
- `WindowFocusChanged` - 仅在进程名变化时触发

### 3.3 ActivityDetector（活动检测器）

**文件**: `src/RecordTime.Core/Services/ActivityDetector.cs`

基于优先级的活动分类：

```
优先级 1: Idle         (系统空闲 > 300 秒)
优先级 2: Video        (媒体会话 / 视频应用+音频 / 浏览器视频)
优先级 3: Gaming       (全屏+频繁输入 / 游戏平台进程)
优先级 4: ActiveTyping (键盘 > 20次/30s 或 组合输入)
优先级 5: PassiveBrowsing (默认)
```

**应用分类**：
- 开发工具：code, devenv, idea, pycharm...
- 办公软件：WINWORD, EXCEL, POWERPNT...
- 视频娱乐：vlc, potplayer, bilibili...
- 社交通讯：WeChat, QQ, telegram...
- 游戏娱乐：steam, epicgameslauncher...

### 3.4 InputMonitor（输入监控器）

**文件**: `src/RecordTime.Core/Services/InputMonitor.cs`

跟踪用户输入活动：

| 指标 | 窗口 |
|------|------|
| 键盘事件计数 | 30 秒滚动窗口 |
| 鼠标点击计数 | 30 秒滚动窗口 |
| 鼠标移动距离 | 30 秒滚动窗口 |
| 系统空闲时间 | GetLastInputInfo() |

### 3.5 MediaDetector（媒体检测器）

**文件**: `src/RecordTime.Core/Services/MediaDetector.cs`

多方式检测视频播放：
- Windows Media Session API
- Audio Session 枚举
- 进程名匹配

## 4. UI 层设计

### 4.1 ViewModel 结构

| ViewModel | 职责 |
|-----------|------|
| MainWindowViewModel | 主窗口协调、监控控制、页面导航 |
| AppStatsViewModel | 应用统计展示 |
| ReportViewModel | 图表分析、AI 报告生成 |
| TimeBudgetViewModel | 时间预算管理 |
| SettingsViewModel | 设置管理 |
| AboutViewModel | 关于页面 |

### 4.2 服务架构

```
┌─────────────────────────────────────────────────────┐
│                 UI Services                          │
├─────────────────┬─────────────────┬─────────────────┤
│ TrayIconService │ AppDataService  │ IconExtractor   │
│ (系统托盘)       │ (数据聚合)       │ (图标提取)      │
├─────────────────┼─────────────────┼─────────────────┤
│ BudgetTracking  │ Notification    │ DailySummary    │
│ Service         │ Service         │ Service         │
│ (预算追踪)       │ (通知提醒)       │ (日末总结)      │
├─────────────────┴─────────────────┴─────────────────┤
│               GoalSuggestionEngine                   │
│                  (AI 目标建议)                        │
└─────────────────────────────────────────────────────┘
```

### 4.3 多语言系统

**文件**: `src/RecordTime.Avalonia/Resources/Strings/StringResources.cs`

```csharp
// 单例模式全局访问
StringResources.Current.AppTitle

// 运行时语言切换
StringResources.Current.SwitchLanguage("en-us")
```

支持语言：
- 中文 (zh-CN) - 默认
- English (en-US)

## 5. 数据层设计

### 5.1 数据模型

#### AppSession（应用会话）

```csharp
public class AppSession
{
    public int Id { get; set; }
    public string ProcessName { get; set; }
    public string DisplayName { get; set; }
    public string? Category { get; set; }
    public ActivityType ActivityType { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSeconds { get; set; }
    public string? WindowTitleHash { get; set; }  // SHA256 加密
    public DateTime LastHeartbeat { get; set; }   // 心跳时间戳
}
```

#### TimeBudget（时间预算）

```csharp
public class TimeBudget
{
    public int Id { get; set; }
    public string? ProcessName { get; set; }      // 应用预算
    public string? Category { get; set; }         // 分类预算
    public BudgetType Type { get; set; }          // Maximum/Minimum
    public int TargetMinutes { get; set; }
    public bool ReminderEnabled { get; set; }
    public int ReminderThreshold { get; set; }    // 提醒阈值 (%)
}
```

### 5.2 数据库索引

```sql
IX_Sessions_EndTime           -- 未完成会话查询
IX_Sessions_LastHeartbeat     -- 过期会话检测
IX_Sessions_StartTime_EndTime -- 日期范围查询
IX_Sessions_ProcessName       -- 应用统计查询
```

### 5.3 Repository 模式

```csharp
public interface ISessionRepository
{
    Task<AppSession> CreateSessionAsync(AppSession session);
    Task EndSessionAsync(int sessionId, DateTime endTime);
    Task UpdateHeartbeatAsync(int sessionId);
    Task<List<AppSession>> GetSessionsAsync(DateTime start, DateTime end);
    // ...
}
```

## 6. 数据完整性机制

### 6.1 心跳机制

```
┌──────────────────────────────────────────────────────┐
│  SessionManager                                       │
│                                                       │
│  ┌─────────────────┐     每30秒      ┌────────────┐ │
│  │ HeartbeatTimer  │ ───────────────> │ 更新       │ │
│  └─────────────────┘                  │ LastHeartbeat│ │
│                                       └────────────┘ │
└──────────────────────────────────────────────────────┘
```

### 6.2 启动自动修复

```csharp
// MainWindowViewModel.RepairIncompleteSessions()
1. 查找 EndTime IS NULL 的会话
2. 检查 LastHeartbeat 是否过期 (> 2分钟)
3. 过期会话：EndTime = LastHeartbeat
4. 记录修复日志
```

## 7. 时间预算系统

### 7.1 架构

```
┌─────────────────────────────────────────────────────┐
│              BudgetTrackingService                   │
│                                                      │
│  Timer (60s) ──> CalculateProgress() ──> Events    │
│                        │                             │
│                        ▼                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐ │
│  │ProgressUpdate│  │ReminderTriggered│ │GoalMet    │ │
│  │ Event       │  │ Event         │  │ Event     │ │
│  └─────────────┘  └─────────────┘  └─────────────┘ │
└─────────────────────────────────────────────────────┘
           │                 │
           ▼                 ▼
┌─────────────────┐  ┌─────────────────┐
│ UI 进度更新     │  │ NotificationService│
└─────────────────┘  └─────────────────┘
```

### 7.2 GoalSuggestionEngine

基于历史数据的 AI 建议：
- 分析过去 7 天使用数据
- 娱乐应用：建议减少 25%
- 生产力应用：建议增加 15%
- 缓存建议避免重复计算

## 8. 隐私保护设计

| 措施 | 实现 |
|------|------|
| 窗口标题加密 | SHA256 哈希 (`SessionManager.HashWindowTitle()`) |
| 本地存储 | SQLite @ `%LOCALAPPDATA%\RecordTime\` |
| AI 数据脱敏 | 自动移除 URL、邮箱等敏感信息 |
| 可选 AI | 默认关闭，用户控制 |

## 9. 性能优化

| 优化项 | 实现 |
|--------|------|
| 查询优化 | EF Core `AsNoTracking()` |
| 线程安全 | `Interlocked` 计数器 |
| 资源管理 | `await using` / `using` |
| 数据缓存 | 5 分钟过期缓存 |
| 轮询优化 | 500ms 间隔 (从 2s 优化) |

## 10. 扩展点

### 添加新的活动类型

1. 在 `ActivityType` 枚举添加新类型
2. 在 `ActivityDetector.DetermineActivity()` 添加检测逻辑
3. 更新 UI 显示和多语言资源

### 添加新的监控服务

1. 实现 `IMonitorService` 接口
2. 在 `SessionManager` 中注入并订阅事件
3. 在 `SystemState` 中添加相关状态字段

### 添加新语言

1. 创建 `NewLanguageStrings.cs` 实现 `IStringProvider`
2. 在 `StringResources.SwitchLanguage()` 添加语言代码
3. 在设置页面添加语言选项
