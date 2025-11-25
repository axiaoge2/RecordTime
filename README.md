# RecordTime - Windows桌面时间追踪工具

## 📝 项目简介

RecordTime 是一款专为Windows平台打造的桌面应用使用时长追踪工具。它能够智能记录您的应用使用习惯,通过可视化数据分析和AI智能建议,帮助您更好地了解时间分配,提升工作效率。

### ✨ 核心特性

- **智能监控** - 自动识别并记录桌面应用使用时长,支持实时数据更新
- **多场景识别** - 准确识别视频播放、游戏、主动输入、被动浏览、空闲等5种活动类型
- **可视化仪表盘** - 实时展示今日使用统计、TOP应用排行、分类时长分布
- **趋势分析图表** - LiveCharts驱动的7天趋势图和活动类型分布饼图
- **系统托盘集成** - 支持后台运行、开机自启动、托盘快捷操作
- **多语言支持** - 中文(zh-CN)和英文(en-US)界面切换
- **AI智能分析** - 支持OpenAI API和自定义模型,生成HTML时间分析报告
- **本地化存储** - 所有数据保存在本地SQLite数据库,保护您的隐私
- **数据完整性保障** - 心跳机制防止数据丢失,自动修复异常会话

## 🎯 功能亮点

### 1. 实时监控仪表盘

- **今日统计卡片** - 总时长、会话数、应用数、活动类型数一目了然
- **TOP 10 应用排行** - 显示应用图标、使用时长、百分比
- **分类时长分布** - 按应用类别(开发工具、办公软件、视频娱乐等)统计
- **大型圆环图** - 可视化展示主要应用类别的时长占比
- **日期导航** - 快速查看历史数据,支持前一天/后一天/今天切换
- **实时刷新** - 监控运行时自动刷新数据,非监控状态显示历史数据

### 2. 多场景活动识别

- **视频播放** - 通过Windows Media Session API和音频会话检测
- **游戏娱乐** - 全屏检测+频繁输入识别游戏场景
- **主动输入** - 基于键鼠交互频率判断主动工作状态(>20键/30s或>10键+>5点击/30s)
- **被动浏览** - 窗口焦点但低活动量的浏览场景
- **空闲状态** - 系统空闲超过5分钟自动标记(视频播放和在线会议豁免)

### 3. 数据可视化与分析

- **7天趋势折线图** - 展示每日使用时长变化趋势,支持自定义日期范围
- **活动类型分布饼图** - 可视化展示视频、游戏、主动输入等活动占比
- **应用统计页面** - 详细的应用使用排行和分类统计
- **日期范围选择** - 支持自定义时间范围查询(最近7天/1个月/3个月)
- **数据缓存优化** - 5分钟缓存机制提升图表加载性能

### 4. AI时间分析

- **多配置管理** - 支持保存多个AI配置(OpenAI、Azure OpenAI、自定义API)
- **配置切换与重命名** - 快速切换不同API配置,支持配置重命名
- **隐私级别控制** - 3级隐私保护(仅分类/包含应用名/包含时长详情)
- **HTML报告生成** - 生成包含AI分析建议的精美HTML报告
- **连接测试** - 保存前测试API连接有效性,避免配置错误
- **智能建议** - 基于使用模式提供个性化时间管理建议
- **可选功能** - AI分析默认关闭,完全由用户控制

### 5. 隐私保护

- **窗口标题哈希** - 使用SHA256加密存储敏感标题
- **本地数据库** - SQLite数据库存储在 `%LOCALAPPDATA%\RecordTime\recordtime.db`
- **数据脱敏** - AI分析前自动移除URL、邮箱等敏感信息
- **可选AI分析** - AI功能默认关闭,完全由用户控制
- **透明日志** - Serilog详细记录在 `%LOCALAPPDATA%\RecordTime\Logs\`,便于审计

### 6. 数据完整性保障

- **心跳机制** - 每30秒更新会话心跳,防止崩溃导致时长异常
- **自动修复** - 启动时自动检测并修复未正常结束的会话
- **空闲检查** - 2分钟间隔检查空闲状态,自动暂停长时间空闲会话
- **优化索引** - 数据库索引优化查询性能(`IX_Sessions_EndTime`, `IX_Sessions_LastHeartbeat`, `IX_Sessions_StartTime_EndTime`)

## 🛠️ 技术架构

### 技术栈

- **框架**: .NET 7.0 + Avalonia UI 11.3.8
- **数据库**: SQLite + Entity Framework Core 7.0 (Code First Migrations)
- **MVVM**: CommunityToolkit.Mvvm 8.2.1
- **图表**: LiveChartsCore.SkiaSharpView.Avalonia 2.0.0-rc6.1
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **日志**: Serilog 4.3.0 (Console + File Sinks)
- **性能优化**: AsNoTracking 查询、Interlocked 线程安全、资源自动释放

### 项目结构

```
RecordTime/
├── src/
│   ├── RecordTime.Avalonia/     # Avalonia UI 前端界面
│   │   ├── Views/               # 页面视图(MainWindow, AppStatsView, ReportView, SettingsView, AboutView)
│   │   ├── ViewModels/          # 视图模型(MVVM)
│   │   ├── Services/            # UI服务(AppDataService, TrayIconService, IconExtractor)
│   │   ├── Resources/Strings/   # 多语言资源(ChineseStrings, EnglishStrings)
│   │   └── Assets/Icons/        # 应用图标和托盘图标
│   ├── RecordTime.Core/         # 核心业务逻辑
│   │   ├── Models/              # 数据模型(AppSession, ActivityType, DailyUsage, AI模型)
│   │   ├── Services/            # 监控服务(SessionManager, WindowMonitor, InputMonitor, MediaDetector, ActivityDetector)
│   │   └── Exceptions/          # 自定义异常类型
│   ├── RecordTime.Data/         # 数据访问层
│   │   ├── Repositories/        # 数据仓储(SessionRepository)
│   │   ├── Reports/             # 报告生成(HtmlReportGenerator, ReportDataBuilder)
│   │   ├── Migrations/          # EF Core迁移文件
│   │   └── RecordTimeDbContext.cs # DbContext配置
│   ├── RecordTime.Console/      # 控制台测试工具
│   └── RecordTime.UI/           # (已弃用) WinUI 3 旧版本
├── tools/                       # 验证工具
│   ├── VerifyHeartbeat/         # 验证心跳机制
│   ├── VerifyIndexes/           # 验证数据库索引
│   ├── CheckHeartbeatDetail/    # 检查心跳详情
│   ├── DatabaseCleanup/         # 数据库清理工具
│   └── ...                      # 其他工具
├── build/                       # 构建输出
├── docs/                        # 文档
└── tests/                       # 单元测试(待完善)
```

### 核心组件

#### 1. SessionManager (核心会话管理器)
`src/RecordTime.Core/Services/SessionManager.cs`

- 整合所有监控服务的中央协调器
- 订阅 `WindowMonitor.WindowFocusChanged` 事件
- 使用 `ActivityDetector` 分类活动类型
- 通过 `SessionRepository` 持久化会话数据
- 实现30秒心跳机制防止数据丢失
- 2分钟间隔空闲检查,支持视频播放豁免

#### 2. WindowMonitor (窗口监控)
`src/RecordTime.Core/Services/WindowMonitor.cs`

- 使用Win32 API轮询前台窗口(每500ms)
- `GetForegroundWindow()` 获取活动窗口
- `GetWindowThreadProcessId()` 识别进程
- 仅在进程名变化时触发 `WindowFocusChanged` 事件
- 性能优化:从2秒降至500ms,提升响应性

#### 3. ActivityDetector (活动类型检测)
`src/RecordTime.Core/Services/ActivityDetector.cs`

优先级检测规则:
1. **Idle** - 系统空闲 > 5分钟
2. **Video** - 媒体会话播放 OR (视频应用 + 音频) OR (浏览器 + 视频)
3. **Gaming** - 全屏 + 频繁输入 OR 游戏平台进程名
4. **ActiveTyping** - 键盘活动 > 20次/30s OR (>10次键盘 + >5次点击/30s)
5. **PassiveBrowsing** - 窗口聚焦但低活动量

应用分类: 开发工具、办公软件、视频娱乐、社交通讯、游戏娱乐、系统工具等

#### 4. MediaDetector (媒体检测)
`src/RecordTime.Core/Services/MediaDetector.cs`

- Windows Media Session API监听媒体播放
- Audio Session枚举检测音频活动
- 进程名匹配识别视频播放器

#### 5. InputMonitor (输入监控)
`src/RecordTime.Core/Services/InputMonitor.cs`

- 键盘事件计数(30秒滚动窗口)
- 鼠标点击计数(30秒滚动窗口)
- 鼠标移动距离跟踪
- 通过 `GetLastInputInfo()` 获取系统空闲时间

#### 6. MainWindowViewModel (主窗口视图模型)
`src/RecordTime.Avalonia/ViewModels/MainWindowViewModel.cs`

- MVVM架构的主要协调者
- 管理监控状态和数据刷新
- LiveCharts图表数据绑定
- 单例模式管理子页面ViewModel

#### 7. ReportViewModel (报告视图模型)
`src/RecordTime.Avalonia/ViewModels/ReportViewModel.cs`

- 7天趋势图和活动分布图表
- AI配置管理(多配置支持)
- HTML报告生成
- 数据缓存优化(5分钟过期)

#### 8. AppDataService (应用数据服务)
`src/RecordTime.Avalonia/Services/AppDataService.cs`

- 单例模式的数据聚合服务
- 计算今日统计和应用排行
- 数据快照机制避免重复查询
- 桥接ViewModel和Data层

#### 9. TrayIconService (托盘图标服务)
`src/RecordTime.Avalonia/Services/TrayIconService.cs`

- 系统托盘集成和最小化到托盘
- 开机自启动(通过Windows注册表)
- 托盘菜单:显示窗口、启动/停止监控、开机自启、退出
- 动态图标:监控中/未监控状态切换

#### 10. StringResources (多语言系统)
`src/RecordTime.Avalonia/Resources/Strings/StringResources.cs`

- 单例模式全局访问
- 支持中文(zh-CN)和英文(en-US)
- 运行时语言切换
- 语言偏好持久化

## 📋 系统要求

### 最低要求
- **操作系统**: Windows 10 版本 1809 (Build 17763) 或更高
- **推荐系统**: Windows 10 21H2 / Windows 11
- **.NET运行时**: .NET 7.0 Runtime
- **内存**: 最少 512MB RAM
- **磁盘空间**: 100MB

### 开发环境
- **.NET SDK**: .NET 7.0 SDK
- **IDE**: Visual Studio Code (推荐) 或 Visual Studio 2022
- **注意**: Avalonia 项目可以在 VS Code 或 Visual Studio 中开发

### 兼容性说明

✅ **支持的Windows版本**
- Windows 11 (所有版本)
- Windows 10 版本 1809 及以上

❌ **不支持**
- Windows 8.1 及更早版本
- Windows 10 版本 1803 及更早版本

## 🚀 快速开始

### 安装依赖

1. 安装 .NET 7 SDK (已安装可跳过)
```bash
# 下载地址: https://dotnet.microsoft.com/download/dotnet/7.0
# 验证安装: dotnet --version
```

2. 克隆项目
```bash
git clone https://github.com/yourusername/recordtime.git
cd recordtime
```

3. 还原NuGet包
```bash
dotnet restore
```

### 编译运行

```bash
# 编译项目
dotnet build

# 运行 Avalonia UI 应用
dotnet run --project src/RecordTime.Avalonia

# 编译 Release 版本
dotnet build -c Release

# 发布单文件自包含程序
dotnet publish src/RecordTime.Avalonia -c Release -r win-x64 --self-contained true
```

### 数据库管理

首次运行时会自动应用数据库迁移并创建数据库:
```
%LOCALAPPDATA%\RecordTime\recordtime.db
```

**EF Core Migrations 管理**:
```bash
# 查看迁移历史
dotnet ef migrations list --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia

# 添加新迁移
dotnet ef migrations add MigrationName --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia

# 应用迁移
dotnet ef database update --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia

# 移除最后一个迁移(未应用时)
dotnet ef migrations remove --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia
```

**重要**:创建迁移前请确保终止所有RecordTime实例,避免DLL锁定问题。

### 验证工具

`tools/` 目录包含多个验证工具:
```bash
# 验证心跳机制
dotnet run --project tools/VerifyHeartbeat

# 验证数据库索引
dotnet run --project tools/VerifyIndexes

# 检查心跳详情
dotnet run --project tools/CheckHeartbeatDetail

# 数据库清理
dotnet run --project tools/DatabaseCleanup
dotnet run --project tools/CleanupTestData
```

## 📊 数据模型

### AppSession (应用会话)
```csharp
{
    "Id": 12345,
    "ProcessName": "chrome",
    "DisplayName": "Google Chrome",
    "ActivityType": "Video",
    "StartTime": "2024-11-25T14:30:00",
    "EndTime": "2024-11-25T15:45:00",
    "DurationSeconds": 4500,
    "Category": "视频娱乐",
    "Confidence": 90,
    "WindowTitleHash": "base64EncodedSHA256Hash",
    "LastHeartbeat": "2024-11-25T15:45:00"
}
```

### ActivityType (活动类型)
```csharp
public enum ActivityType
{
    Video,              // 视频播放
    Gaming,             // 游戏娱乐
    ActiveTyping,       // 主动输入
    PassiveBrowsing,    // 被动浏览
    Idle                // 空闲
}
```

## 🔐 隐私承诺

1. **本地存储** - 数据永不上传云端(除非您主动启用AI分析)
2. **敏感信息过滤** - 自动移除URL、邮箱等隐私数据
3. **加密哈希** - 窗口标题使用SHA256哈希存储
4. **透明权限** - 清晰说明需要的系统权限及用途
5. **日志审计** - 所有操作记录在本地日志,便于审计

## 🗺️ 开发路线图

### Phase 1 - 数据完整性 ✅ (2024-11-16)
- [x] 添加 `LastHeartbeat` 列
- [x] 实现30秒心跳机制
- [x] 启动时自动修复异常会话
- [x] 优化数据库索引
- [x] 创建验证工具

### Phase 2 - 系统托盘与图标 ✅ (2024-11-18)
- [x] 系统托盘集成
- [x] 最小化到托盘行为
- [x] 开机自启动功能
- [x] 应用图标提取
- [x] WindowMonitor优化至500ms

### Phase 3 - UI/分析增强 ✅ (2024-11-23)
- [x] 实时监控仪表盘
- [x] 7天趋势图(LiveCharts)
- [x] 活动类型分布可视化
- [x] 设置页面(语言选择)
- [x] AI分析功能
- [x] 多语言支持(中英文)
- [x] AppDataService数据聚合

### Phase 4 - 高级分析 (规划中)
- [ ] 自定义日期范围过滤
- [ ] 周对周/月对月比较
- [ ] 自动见解生成
- [ ] 数据导出(CSV, Excel)
- [ ] PDF报告生成

### Phase 5 - 行为干预 (规划中)
- [ ] 使用时长目标设定
- [ ] 接近限制时通知
- [ ] 专注模式
- [ ] 每日/每周摘要通知

详见 `PHASE3_PLANNING.md` 获取详细路线图。

## 📝 项目文档

- **CLAUDE.md** - 项目架构分析,开发指南,命令参考
- **AGENTS.md** - 代码规范,提交约定,测试指南
- **README.md** - 本文档,用户入门和功能概览
- **PHASE3_PLANNING.md** - 详细开发路线图和技术规范
- **完整修复总结.md** - P0和P1问题修复记录
- **P1问题修复总结.md** - 性能优化文档
- **ICON_REDESIGN_SUMMARY.md** - 图标设计和资产生成流程

## 🤝 贡献指南

欢迎提交Issue和Pull Request!

开发前请阅读:
1. 使用 EF Core Migrations 管理数据库变更
2. 所有数据库查询应使用 `AsNoTracking()` (除非需要修改实体)
3. 使用 `Interlocked` 实现线程安全的计数器
4. 资源释放优先使用 `await using` 或 `using` 语句
5. 保持代码风格一致,遵循C# Coding Conventions
6. 提交信息使用现在时态,命令式(如 "Add feature", "Fix bug")
7. 运行 `dotnet format` 格式化代码

## 📄 许可证

MIT License

## 👨‍💻 作者

个人项目 - 学习交流用途

---

**最近更新**: 2024-11-25 - 完成Phase 3 UI/分析增强,添加多语言支持和AI分析功能
**项目版本**: 1.0.0
**代码质量**: 8.5/10 (Phase 3优化后从6.5提升)
