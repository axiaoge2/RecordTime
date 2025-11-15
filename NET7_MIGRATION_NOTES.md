# .NET 7.0 降级说明

## ✅ 已完成的降级工作

项目已成功从 .NET 8.0 降级到 .NET 7.0，以兼容您的开发环境。

### 修改的文件

1. **RecordTime.Core.csproj**
   - TargetFramework: `net8.0-windows` → `net7.0-windows10.0.19041.0`
   - Microsoft.Extensions.* 包: `8.0.0` → `7.0.0`
   - 添加了 `Microsoft.WindowsDesktop.App.WindowsForms` 引用

2. **RecordTime.Data.csproj**
   - TargetFramework: `net8.0-windows` → `net7.0-windows10.0.19041.0`
   - EF Core 包: `8.0.0` → `7.0.20`

3. **RecordTime.UI.csproj**
   - TargetFramework: `net8.0-windows` → `net7.0-windows10.0.19041.0`
   - WindowsAppSDK: `1.5.x` → `1.4.231115000`

4. **WindowMonitor.cs**
   - 修复了方法名冲突问题
   - Win32 API声明: `GetForegroundWindow` → `GetForegroundWindow_Native`

## 📦 编译状态

### ✅ 成功编译
- `RecordTime.Core` - 核心业务逻辑层
- `RecordTime.Data` - 数据访问层

### ⚠️ 需要额外配置
- `RecordTime.UI` - WinUI 3 界面层

## 🛠️ 解决UI项目编译问题

UI项目需要Visual Studio的特定组件才能编译。有两种解决方案：

### 方案1: 安装Visual Studio 2022 (推荐)

1. 下载 Visual Studio 2022 Community (免费)
   - 地址: https://visualstudio.microsoft.com/zh-hans/downloads/

2. 在安装程序中选择以下工作负载:
   - ✅ `.NET桌面开发`
   - ✅ `通用Windows平台开发` (UWP)

3. 在"单个组件"标签页中确保已选:
   - ✅ `Windows 11 SDK (10.0.22621.0)`
   - ✅ `Windows App SDK C# 模板`
   - ✅ `.NET 7.0 Runtime`

4. 完成安装后，使用Visual Studio打开解决方案:
   ```
   双击打开: E:\recordtime\RecordTime.sln
   ```

5. 在Visual Studio中按 `Ctrl+Shift+B` 编译

### 方案2: 仅使用命令行 (限制较多)

如果不想安装Visual Studio，可以尝试：

```bash
# 1. 安装Windows SDK
# 下载地址: https://developer.microsoft.com/zh-cn/windows/downloads/windows-sdk/

# 2. 仅编译核心和数据层(不编译UI)
cd E:\recordtime
dotnet build src/RecordTime.Core
dotnet build src/RecordTime.Data

# 注意: WinUI 3项目强烈建议使用Visual Studio
```

## 🔍 当前可用功能

即使UI项目未编译，核心功能仍然可用：

```bash
# 测试核心监控功能
cd E:\recordtime\src\RecordTime.Core
dotnet test  # (如果添加了测试项目)

# 使用控制台测试窗口监控
# 可以创建一个控制台项目引用Core来测试
```

## 📝 版本兼容性说明

### .NET 7.0 与 .NET 8.0 的差异

对于本项目的影响很小：

| 功能 | .NET 7 | .NET 8 | 影响 |
|------|--------|--------|------|
| Windows API调用 | ✅ | ✅ | 无影响 |
| EF Core | ✅ 7.0.20 | ✅ 8.0 | 功能相同 |
| WinUI 3 | ✅ 1.4.x | ✅ 1.5.x | 小差异 |
| 性能 | 良好 | 略优 | 可忽略 |

### WinUI 3 版本差异

- **1.5.x** (.NET 8): 最新版本，支持更多功能
- **1.4.x** (.NET 7): 稳定版本，功能完整

对于本项目使用的基础功能（导航、卡片、按钮等），两个版本没有区别。

## 🚀 下一步建议

### 选项A: 使用Visual Studio (强烈推荐)

优点:
- 完整的智能提示和调试体验
- WinUI 3 XAML设计器
- 一键编译运行
- 性能分析工具

缺点:
- 需要下载约10GB安装包

**推荐给**: 想要完整开发体验的开发者

### 选项B: 仅开发后端逻辑

如果暂时无法安装Visual Studio:

1. 专注于实现核心监控逻辑
   - `MediaDetector` (媒体检测)
   - `InputMonitor` (输入监控)
   - `SessionManager` (会话管理)

2. 使用控制台程序测试

3. 等有了Visual Studio后再完善UI

**推荐给**: 想先实现核心功能的开发者

## 📋 验证编译

```bash
# 验证核心层和数据层可以编译
cd E:\recordtime

# 清理旧文件
dotnet clean

# 编译核心层
dotnet build src/RecordTime.Core --configuration Release

# 编译数据层
dotnet build src/RecordTime.Data --configuration Release

# 如果安装了Visual Studio，编译UI层
dotnet build src/RecordTime.UI --configuration Release
```

## ❓ 常见问题

### Q: 为什么不能用VS Code?
A: WinUI 3需要特定的MSBuild任务和工具链，这些仅在Visual Studio中完整支持。VS Code可以编辑代码，但无法编译UI项目。

### Q: 能否升级到.NET 8?
A: 可以！只需要：
```bash
# 1. 安装.NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# 2. 恢复之前的.csproj文件
# 所有的 net7.0 改回 net8.0
# 包版本改回 8.0.0
```

### Q: 不用WinUI 3可以吗?
A: 可以改用WPF或Windows Forms，但：
- 失去macOS风格的现代UI
- 需要重写所有XAML代码
- 不推荐，除非有特殊需求

## 📞 获取帮助

如果遇到编译问题：

1. 检查.NET版本: `dotnet --version`
2. 清理并重建: `dotnet clean && dotnet build`
3. 查看详细错误: `dotnet build --verbosity detailed`

---

**总结**: 核心代码已成功降级到.NET 7，可以正常开发。要编译UI项目，请安装Visual Studio 2022。
