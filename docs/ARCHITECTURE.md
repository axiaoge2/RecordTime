# RecordTime 架构设计文档

## 1. 整体架构

RecordTime采用**三层架构** + **MVVM模式**设计:

```
┌─────────────────────────────────────────────────────┐
│              Presentation Layer (UI)                │
│         WinUI 3 + MVVM + Data Binding              │
│  ┌─────────┐  ┌──────────┐  ┌─────────────────┐   │
│  │ Views   │  │ViewModels│  │ Converters      │   │
│  └─────────┘  └──────────┘  └─────────────────┘   │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│            Business Logic Layer (Core)              │
│       服务层 + 业务规则 + 领域模型                     │
│  ┌─────────────────┐  ┌──────────────────────┐     │
│  │ WindowMonitor   │  │ ActivityDetector     │     │
│  │ InputMonitor    │  │ MediaDetector        │     │
│  └─────────────────┘  └──────────────────────┘     │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│            Data Access Layer (Data)                 │
│       EF Core + Repository + 数据服务                │
│  ┌──────────────┐  ┌────────────────────────┐      │
│  │ DbContext    │  │ SessionRepository      │      │
│  │ Repositories │  │ DataPrivacyService     │      │
│  └──────────────┘  └────────────────────────┘      │
└────────────────────┬────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────┐
│                SQLite Database                      │
│           %LOCALAPPDATA%\RecordTime\               │
└─────────────────────────────────────────────────────┘
```

## 2. 核心模块设计

### 2.1 监控引擎 (Monitoring Engine)

#### WindowMonitor - 窗口监控器
```csharp
职责:
- 轮询前台窗口变化 (2秒间隔)
- 获取窗口标题和进程信息
- 检测全屏状态
- 触发窗口切换事件

实现技术:
- Win32 API: GetForegroundWindow
- Win32 API: GetWindowThreadProcessId
- Timer: 定时轮询机制
```

#### InputMonitor - 输入监控器 (TODO)
```csharp
职责:
- 监听全局键盘事件
- 监听全局鼠标事件
- 统计30秒窗口内活动次数
- 判断系统空闲状态

实现技术:
- Win32 Hooks: SetWindowsHookEx (WH_KEYBOARD_LL)
- Win32 Hooks: SetWindowsHookEx (WH_MOUSE_LL)
- Win32 API: GetLastInputInfo (空闲检测)
```

#### MediaDetector - 媒体检测器 (TODO)
```csharp
职责:
- 监听系统媒体会话
- 检测音频播放状态
- 监听ETW视频事件
- 识别浏览器视频播放

实现技术:
- Windows.Media.Control.GlobalSystemMediaTransportControlsSessionManager
- Core Audio API: IAudioSessionManager2
- ETW: Microsoft-Windows-MediaFoundation-*
- 进程白名单匹配
```

### 2.2 活动判定引擎

#### ActivityDetector - 活动类型判定器

```csharp
判定优先级 (从高到低):

1. Video (视频播放) - 置信度90%
   条件: 媒体会话播放 OR (视频应用 + 音频活跃) OR (浏览器 + 视频播放)

2. Gaming (游戏) - 置信度85%
   条件: 全屏 + 高GPU使用率 + 频繁输入

3. ActiveTyping (主动输入) - 置信度80%
   条件: 键盘活动>10次/30s OR 鼠标点击>5次/30s

4. PassiveBrowsing (被动浏览) - 置信度60%
   条件: 窗口聚焦 + 系统非空闲

5. Idle (空闲) - 置信度100%
   条件: 其他都不满足
```

**决策树伪代码:**
```
function DetermineActivity(window, state):
    if state.MediaSessionPlaying:
        return Video (confidence=90)

    if state.IsVideoApp AND state.AudioActive:
        return Video (confidence=85)

    if state.IsBrowser AND state.BrowserVideoPlaying:
        return Video (confidence=90)

    if window.IsFullscreen AND state.HighGpu AND state.FrequentInput:
        return Gaming (confidence=85)

    if state.KeyboardActivity > threshold OR state.MouseClicks > threshold:
        return ActiveTyping (confidence=80)

    if state.WindowFocused AND NOT state.SystemIdle:
        return PassiveBrowsing (confidence=60)

    return Idle (confidence=100)
```

### 2.3 数据流设计

#### 数据采集 → 存储流程

```
1. WindowMonitor 检测到窗口切换
   ↓
2. 收集 SystemState (媒体/音频/输入状态)
   ↓
3. ActivityDetector 判定活动类型
   ↓
4. 结束上一个Session,创建新Session
   ↓
5. DataPrivacyService 哈希敏感信息
   ↓
6. SessionRepository 写入数据库
```

#### Session生命周期管理

```csharp
class SessionManager
{
    private AppSession? _currentSession;

    OnWindowChanged(WindowInfo window, SystemState state):
        var activityType = _activityDetector.Determine(window, state);

        if _currentSession != null:
            _currentSession.EndTime = DateTime.Now;
            _currentSession.DurationSeconds = calculate_duration();
            await _repository.UpdateSession(_currentSession);

        _currentSession = new AppSession {
            ProcessName = window.ProcessName,
            StartTime = DateTime.Now,
            ActivityType = activityType,
            WindowTitleHash = HashTitle(window.Title)
        };

        await _repository.AddSession(_currentSession);
}
```

## 3. 数据模型设计

### 3.1 数据库Schema

```sql
-- 应用会话表
CREATE TABLE Sessions (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ProcessName TEXT NOT NULL,
    DisplayName TEXT NOT NULL,
    WindowTitleHash TEXT,
    ActivityType TEXT NOT NULL,  -- Video, ActiveTyping, Gaming, PassiveBrowsing, Idle
    StartTime DATETIME NOT NULL,
    EndTime DATETIME,
    DurationSeconds INTEGER NOT NULL DEFAULT 0,
    Confidence INTEGER NOT NULL,
    Category TEXT,

    -- 索引
    INDEX idx_start_time (StartTime),
    INDEX idx_process_name (ProcessName),
    INDEX idx_activity_type (ActivityType)
);

-- AI分析结果表 (Phase 4)
CREATE TABLE AIInsights (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PeriodStart DATETIME NOT NULL,
    PeriodEnd DATETIME NOT NULL,
    Summary TEXT,
    Recommendations TEXT,  -- JSON格式
    ModelVersion TEXT,
    CreatedAt DATETIME NOT NULL
);
```

### 3.2 数据隐私处理

```csharp
class DataPrivacyService
{
    // 窗口标题哈希
    HashWindowTitle(title):
        1. 移除URL → "[URL]"
        2. 移除邮箱 → "[EMAIL]"
        3. 移除IP → "[IP]"
        4. SHA256(sanitized_title + user_salt)

    // AI分析数据脱敏
    PrepareForAI(sessions):
        return {
            "total_time": "8h 32m",  // 聚合数据
            "categories": {
                "视频娱乐": "3h 15m",
                "开发工具": "2h 40m"
            },
            // 不包含具体进程名、窗口标题
        }
}
```

## 4. UI架构设计

### 4.1 MVVM模式

```
View (XAML)
    ↕ DataBinding
ViewModel (逻辑)
    ↕ Services
Model (数据)
```

### 4.2 页面结构

```
MainWindow
├── NavigationView (左侧导航)
│   ├── OverviewPage (概览)
│   │   ├── TodayStats (今日统计)
│   │   ├── RecentApps (最近应用)
│   │   └── QuickActions (快捷操作)
│   │
│   ├── AppsPage (应用列表)
│   │   ├── AppList (所有应用)
│   │   └── CategoryFilter (分类筛选)
│   │
│   ├── ReportsPage (报告)
│   │   ├── DateRangePicker (日期选择)
│   │   ├── Charts (图表展示)
│   │   └── ExportButton (导出CSV)
│   │
│   ├── AIAnalysisPage (AI分析)
│   │   ├── AnalysisResults (分析结果)
│   │   ├── Recommendations (建议)
│   │   └── ModelSettings (模型设置)
│   │
│   └── SettingsPage (设置)
│       ├── MonitoringSettings (监控设置)
│       ├── PrivacySettings (隐私设置)
│       └── AISettings (AI配置)
```

### 4.3 macOS风格设计规范

```xaml
<!-- 配色方案 -->
AccentColor: #007AFF (iOS蓝)
Background: #F5F5F7 (浅灰)
CardBackground: #FFFFFF (纯白)
TextPrimary: #000000
TextSecondary: #6E6E73

<!-- 圆角规范 -->
Button: 6dp
Card: 12dp
Dialog: 16dp

<!-- 间距规范 -->
Small: 8dp
Medium: 16dp
Large: 24dp

<!-- 字体 -->
Windows: Segoe UI Variable
macOS等效: SF Pro
```

## 5. 性能优化策略

### 5.1 监控性能优化

```csharp
// 1. 合理的轮询间隔
WindowMonitor: 2秒 (平衡准确性和性能)
InputMonitor: 事件驱动 (不轮询)

// 2. 事件节流
InputEvents: 每秒最多采样10次

// 3. 批量写入
每60秒批量提交数据库,而非实时写入
```

### 5.2 内存优化

```csharp
// 1. 使用struct代替class (小对象)
struct InputEvent { ... }

// 2. 对象池复用
ObjectPool<SystemState> statePool;

// 3. 及时释放资源
using var session = _audioManager.GetSessions();
```

## 6. 安全性设计

### 6.1 权限最小化原则

```
需要的权限:
✓ 进程信息读取
✓ 窗口信息读取
✓ 音频会话查询
✓ 文件系统访问 (LocalAppData)

不需要的权限:
✗ 管理员权限
✗ 网络访问 (除非启用AI)
✗ 屏幕截图
✗ 键盘记录内容 (只统计次数)
```

### 6.2 数据加密 (可选)

```csharp
// Phase 5: 数据库加密
optionsBuilder.UseSqlite("Data Source=recordtime.db;Password=xxx");
```

## 7. 扩展性设计

### 7.1 插件化接口 (未来)

```csharp
interface IActivityPlugin
{
    string Name { get; }
    ActivityType Detect(WindowInfo window, SystemState state);
    int Priority { get; }  // 优先级
}

class PluginManager
{
    RegisterPlugin(IActivityPlugin plugin);
    UnregisterPlugin(string name);
}
```

### 7.2 配置化规则

```json
{
  "video_apps": ["vlc", "potplayer"],
  "work_apps": ["code", "studio"],
  "idle_threshold_seconds": 300,
  "monitoring_interval_ms": 2000
}
```

## 8. 测试策略

### 8.1 单元测试

```csharp
测试覆盖:
- ActivityDetector判定逻辑
- DataPrivacyService脱敏功能
- SessionRepository CRUD操作
```

### 8.2 集成测试

```csharp
测试场景:
- 窗口切换 → 数据库写入
- 视频播放 → 正确识别为Video类型
- 系统空闲 → 正确标记为Idle
```

---

**版本**: v0.1 (Phase 1完成)
**最后更新**: 2025-11-14
