# RecordTime 快速开始指南

## 🎯 项目当前状态

**Phase 1 (基础功能) - 已完成 ✅**

已实现的功能:
- ✅ 完整的项目结构搭建
- ✅ 窗口焦点监控核心代码
- ✅ SQLite数据存储层
- ✅ macOS风格基础UI
- ✅ 数据隐私保护服务
- ✅ 完整的技术文档

## 📋 后续开发计划

### Phase 2 - 视频检测 (下一步)

需要实现:
1. **MediaDetector** - 媒体播放检测
   - Windows Media Session监听
   - Audio Session监控
   - ETW事件处理

2. **InputMonitor** - 输入活动监控
   - 键盘钩子 (WH_KEYBOARD_LL)
   - 鼠标钩子 (WH_MOUSE_LL)
   - 系统空闲检测

3. **SessionManager** - 会话管理器
   - 自动开始/结束会话
   - 后台服务集成

### Phase 3 - UI完善

需要实现:
1. 数据可视化图表
2. 应用列表页面
3. 统计报告页面
4. 设置页面

### Phase 4 - AI分析

需要实现:
1. OpenAI API集成
2. 本地模型支持 (llama.cpp)
3. AI分析UI

## 🛠️ 开发环境设置

### 1. 必需工具

```bash
# 1. 安装.NET 8 SDK
下载: https://dotnet.microsoft.com/download/dotnet/8.0

# 2. 验证安装
dotnet --version  # 应该显示 8.x.x

# 3. (可选) 安装Visual Studio 2022
# - 选择 ".NET桌面开发" 工作负载
# - 选择 "Windows应用SDK" 组件
```

### 2. 克隆和构建

```bash
# 克隆项目
cd E:\recordtime

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行UI项目
dotnet run --project src/RecordTime.UI
```

### 3. 数据库初始化

首次运行时,应用会自动创建数据库:

```
位置: %LOCALAPPDATA%\RecordTime\recordtime.db
完整路径示例: C:\Users\YourName\AppData\Local\RecordTime\recordtime.db
```

**手动创建数据库迁移** (可选):

```bash
# 安装EF Core工具
dotnet tool install --global dotnet-ef

# 创建迁移
cd src/RecordTime.Data
dotnet ef migrations add InitialCreate

# 应用迁移
dotnet ef database update
```

## 📝 开发任务清单

### 立即可做的任务

**任务1: 实现MediaDetector** (难度: ⭐⭐⭐)
```csharp
文件: src/RecordTime.Core/Services/MediaDetector.cs

需要实现:
- SystemMediaTransportControls监听
- IAudioSessionManager2集成
- ETW Provider订阅

参考: docs/ARCHITECTURE.md 第2.1节
```

**任务2: 实现InputMonitor** (难度: ⭐⭐)
```csharp
文件: src/RecordTime.Core/Services/InputMonitor.cs

需要实现:
- SetWindowsHookEx (键盘钩子)
- SetWindowsHookEx (鼠标钩子)
- GetLastInputInfo (空闲检测)

参考: docs/ARCHITECTURE.md 第2.1节
```

**任务3: 完善UI页面** (难度: ⭐⭐)
```csharp
文件: src/RecordTime.UI/Views/

需要创建:
- OverviewPage.xaml (概览页)
- AppsPage.xaml (应用列表)
- ReportsPage.xaml (报告页)
- SettingsPage.xaml (设置页)

参考: docs/ARCHITECTURE.md 第4.2节
```

**任务4: 集成SessionManager** (难度: ⭐⭐⭐)
```csharp
文件: src/RecordTime.Core/Services/SessionManager.cs

需要实现:
- 监听WindowMonitor事件
- 调用ActivityDetector判定
- 自动创建/结束Session
- 写入数据库

参考: docs/ARCHITECTURE.md 第2.3节
```

## 🐛 调试技巧

### 1. 查看监控日志

```csharp
// 在WindowMonitor.cs中添加日志
private void MonitorCallback(object? state)
{
    var window = GetForegroundWindow();
    Debug.WriteLine($"[Monitor] {window?.ProcessName} - {window?.WindowTitle}");
}
```

### 2. 测试数据库

```bash
# 使用DB Browser for SQLite查看数据库
下载: https://sqlitebrowser.org/

# 打开数据库文件
%LOCALAPPDATA%\RecordTime\recordtime.db
```

### 3. 模拟窗口切换

```csharp
// 在MainWindow.xaml.cs中测试
private async void TestMonitoring()
{
    var monitor = App.Services.GetService<IWindowMonitor>();
    monitor.WindowFocusChanged += (s, window) =>
    {
        Debug.WriteLine($"窗口切换: {window.ProcessName}");
    };
    monitor.Start();
}
```

## 📚 学习资源

### WinUI 3开发
- [官方文档](https://learn.microsoft.com/windows/apps/winui/)
- [WinUI 3 Gallery](https://github.com/microsoft/WinUI-Gallery)

### Win32 API
- [GetForegroundWindow](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-getforegroundwindow)
- [SetWindowsHookEx](https://learn.microsoft.com/windows/win32/api/winuser/nf-winuser-setwindowshookexw)

### Entity Framework Core
- [EF Core官方文档](https://learn.microsoft.com/ef/core/)

## ❓ 常见问题

### Q1: 编译错误 "找不到Windows SDK"
```bash
解决方案:
1. 安装 Visual Studio 2022
2. 在安装程序中选择 "Windows 11 SDK (10.0.22621.0)"
```

### Q2: WinUI 3应用无法启动
```bash
解决方案:
1. 确保 Windows版本 >= 10.0.17763
2. 安装 Windows App Runtime
   下载: https://learn.microsoft.com/windows/apps/windows-app-sdk/downloads
```

### Q3: 数据库文件在哪里?
```bash
位置: %LOCALAPPDATA%\RecordTime\recordtime.db

快速打开:
1. Win+R
2. 输入: %LOCALAPPDATA%\RecordTime
3. 回车
```

## 🚀 下一步行动

1. **熟悉项目结构** - 阅读 `docs/ARCHITECTURE.md`
2. **运行现有代码** - 确保能编译和运行
3. **选择一个任务** - 从上面的任务清单中选一个开始
4. **提交代码** - 完成后提交到Git仓库

祝你开发顺利！ 🎉
