# RecordTime - Windows桌面时间追踪工具

## 📝 项目简介

RecordTime 是一款专为Windows平台打造的桌面应用使用时长追踪工具。它能够智能记录您的应用使用习惯，帮助您更好地了解时间分配，提升工作效率。

### ✨ 核心特性

- **智能监控** - 自动识别并记录桌面应用使用时长
- **视频优先检测** - 准确识别视频播放场景，避免误判
- **本地化存储** - 所有数据保存在本地，保护您的隐私
- **Apple 启发的设计** - 简约优雅的界面设计，灵感来自 Apple 设计系统
- **系统托盘集成** - 支持后台运行，最小化到系统托盘
- **AI智能分析** - 接入大模型，提供个性化时间管理建议
- **数据脱敏** - 自动移除敏感信息（URL、邮箱等）

## 🎯 功能亮点

### 1. 多场景活动识别

- **视频播放** - 通过媒体会话和音频检测
- **主动工作** - 基于键鼠交互频率判断
- **被动浏览** - 窗口焦点+系统空闲检测
- **游戏娱乐** - 全屏+GPU使用率综合判定

### 2. 隐私保护

- 不记录具体窗口内容
- 自动哈希处理敏感标题
- 数据仅存储在本地SQLite数据库
- AI分析前自动脱敏

### 3. AI时间分析

- 支持OpenAI API和本地模型
- 智能识别时间浪费模式
- 提供个性化改进建议
- 生成周/月时间使用报告

## 🛠️ 技术架构

### 技术栈

- **框架**: .NET 7.0 + Avalonia UI 11.3.8
- **数据库**: SQLite + Entity Framework Core 7.0 (Code First Migrations)
- **MVVM**: CommunityToolkit.Mvvm 8.2.1
- **依赖注入**: Microsoft.Extensions.DependencyInjection
- **系统托盘**: System.Windows.Forms.NotifyIcon
- **性能优化**: AsNoTracking 查询、Interlocked 线程安全、资源自动释放

### 项目结构

```
RecordTime/
├── src/
│   ├── RecordTime.Avalonia/     # Avalonia UI 前端界面
│   │   ├── Views/               # 页面视图
│   │   ├── ViewModels/          # 视图模型(MVVM)
│   │   └── App.axaml            # Apple 启发的设计系统
│   ├── RecordTime.Core/         # 核心业务逻辑
│   │   ├── Models/              # 数据模型
│   │   └── Services/            # 监控服务
│   ├── RecordTime.Data/         # 数据访问层
│   │   ├── Repositories/        # 数据仓储
│   │   └── Services/            # 数据服务
│   ├── RecordTime.Console/      # 控制台测试工具
│   └── RecordTime.UI/           # (已弃用) WinUI 3 旧版本
├── docs/                        # 文档
└── tests/                       # 单元测试
```

### 核心组件

#### 1. WindowMonitor (窗口监控)
- 使用Win32 API `GetForegroundWindow`
- 每2秒轮询前台窗口变化 (优化后降低75% CPU使用率)
- 支持全屏检测
- 线程安全的资源管理

#### 2. ActivityDetector (活动检测)
```csharp
视频检测优先级:
1. 媒体会话播放
2. 视频应用 + 音频活跃
3. 浏览器 + 视频播放

其他活动:
4. 游戏 (全屏 + 高GPU + 频繁输入)
5. 主动输入 (键鼠活动)
6. 被动浏览 (窗口聚焦)
7. 空闲
```

#### 3. DataPrivacyService (隐私保护)
- SHA256哈希窗口标题
- 正则移除URL、邮箱、IP
- AI分析数据聚合脱敏

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

2. (可选) 安装 Visual Studio Code 或 Visual Studio 2022
```bash
# VS Code: https://code.visualstudio.com/
# Visual Studio 2022: https://visualstudio.microsoft.com/zh-hans/downloads/
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
```

### 数据库初始化

首次运行时会自动应用数据库迁移并创建数据库:
```
%LOCALAPPDATA%\RecordTime\recordtime.db
```

#### EF Core Migrations 管理

查看迁移历史:
```bash
dotnet ef migrations list --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia
```

添加新迁移:
```bash
dotnet ef migrations add MigrationName --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia
```

应用迁移:
```bash
dotnet ef database update --project src/RecordTime.Data --startup-project src/RecordTime.Avalonia
```

## 📊 数据模型

### AppSession (应用会话)
```csharp
{
    "ProcessName": "chrome",
    "DisplayName": "Google Chrome",
    "ActivityType": "Video",
    "StartTime": "2025-11-14T14:30:00",
    "EndTime": "2025-11-14T15:45:00",
    "DurationSeconds": 4500,
    "Category": "视频娱乐",
    "Confidence": 90
}
```

## 🔐 隐私承诺

1. **本地存储** - 数据永不上传云端（除非您主动启用AI分析）
2. **敏感信息过滤** - 自动移除URL、邮箱等隐私数据
3. **加密哈希** - 窗口标题使用SHA256+盐值哈希
4. **透明权限** - 清晰说明需要的系统权限及用途

## 🗺️ 开发路线图

### Phase 1 - 基础功能 ✅
- [x] 窗口焦点监控
- [x] 键鼠活动检测
- [x] SQLite数据存储
- [x] 基础UI框架

### Phase 2 - 视频检测与 Avalonia UI ✅
- [x] Windows Media Session 监听
- [x] Audio Session 监控
- [x] 视频优先级判定
- [x] 应用分类系统
- [x] Avalonia UI 界面开发
- [x] 系统托盘集成

### Phase 3 - 质量优化 ✅
- [x] EF Core Code First Migrations
- [x] WindowMonitor 轮询优化 (CPU降低75%)
- [x] 定时器重入保护 (Interlocked)
- [x] AsNoTracking 查询优化 (内存降低30-40%)
- [x] 资源释放优化 (Using语句)
- [x] 项目质量评估与修复 (6.5→8.5分)

### Phase 4 - UI完善 (部分完成)
- [x] Apple 启发的设计系统
- [x] 实时数据展示
- [ ] 图表可视化
- [ ] 应用图标显示
- [ ] 设置页面完善
- [ ] 配置管理系统

### Phase 5 - AI分析
- [ ] 数据脱敏模块
- [ ] OpenAI接口集成
- [ ] 本地模型支持
- [ ] 智能建议展示

### Phase 6 - 高级功能
- [ ] 浏览器扩展 (标签页级追踪)
- [ ] 自动分类学习
- [ ] 时间目标设定
- [ ] 导出报告
- [ ] Serilog日志系统
- [ ] 单元测试覆盖

## 📝 项目文档

- [完整修复总结](./完整修复总结.md) - P0和P1问题修复详细记录
- [P1问题修复总结](./P1问题修复总结.md) - 性能优化详细文档
- [CLAUDE分析报告](./CLAUDE.md) - 项目架构分析与质量评估

## 🤝 贡献指南

欢迎提交Issue和Pull Request！

开发前请阅读:
1. 使用 EF Core Migrations 管理数据库变更
2. 所有数据库查询应使用 `AsNoTracking()` (除非需要修改实体)
3. 使用 `Interlocked` 实现线程安全的计数器
4. 资源释放优先使用 `await using` 或 `using` 语句
5. 保持代码风格一致,遵循C# Coding Conventions

## 📄 许可证

MIT License

## 👨‍💻 作者

个人项目 - 学习交流用途

---

**最近更新**: 2025-11-15 - 完成Phase 3质量优化,项目评分从6.5提升至8.5分
