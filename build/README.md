# RecordTime 发布说明

本目录包含发布和打包 RecordTime 应用程序所需的脚本和配置文件。

## 📋 文件说明

| 文件 | 说明 |
|------|------|
| `publish.ps1` | 发布脚本，生成单文件可执行程序和便携版 |
| `create-installer.ps1` | 创建 Windows 安装程序 |
| `setup.iss` | Inno Setup 安装脚本配置 |
| `output/` | 发布输出目录（生成） |
| `publish/` | 最终发布文件目录（生成） |

## 🚀 快速开始

### 前置要求

1. **已安装 .NET 7.0 SDK**
   ```powershell
   dotnet --version  # 验证安装
   ```

2. **（可选）已安装 Inno Setup 6**
   仅在需要创建安装程序时必需
   - 下载地址: https://jrsoftware.org/isdl.php
   - 默认安装路径: `C:\Program Files (x86)\Inno Setup 6\`

### 步骤 1: 发布应用程序

在项目根目录运行：

```powershell
# 使用默认配置（Release，版本 1.0.0）
.\build\publish.ps1

# 或指定版本号
.\build\publish.ps1 -Version "1.0.1"

# 指定配置
.\build\publish.ps1 -Configuration Release -Version "1.0.0"
```

**输出文件：**
- `build/output/RecordTime.exe` - 单文件可执行程序
- `build/publish/RecordTime-v1.0.0-Portable.zip` - 便携版压缩包

### 步骤 2: 创建安装程序（可选）

```powershell
# 使用默认 Inno Setup 路径
.\build\create-installer.ps1

# 或指定自定义路径
.\build\create-installer.ps1 -InnoSetupPath "C:\Your\Path\ISCC.exe"
```

**输出文件：**
- `build/publish/RecordTime-v1.0.0-Setup.exe` - Windows 安装程序

## 📦 发布文件说明

### 1. 单文件可执行程序 (`RecordTime.exe`)

- **大小**: ~80-120 MB
- **优点**:
  - 包含所有依赖，无需安装 .NET Runtime
  - 解压即用，真正的便携版
- **适用场景**:
  - 技术用户
  - 需要便携使用
  - 不想安装的用户

### 2. 便携版压缩包 (`RecordTime-*-Portable.zip`)

- **内容**: 包含 RecordTime.exe 和配置文件
- **使用方法**:
  1. 解压到任意目录
  2. 双击 `RecordTime.exe` 运行
- **数据位置**: `%LOCALAPPDATA%\RecordTime\`

### 3. 安装程序 (`RecordTime-*-Setup.exe`)

- **大小**: ~50-80 MB（压缩后）
- **功能**:
  - 标准 Windows 安装向导
  - 自动创建桌面快捷方式
  - 开始菜单集成
  - 卸载程序
  - 检测旧版本并提示
- **适用场景**:
  - 普通用户
  - 需要系统集成
  - 希望标准安装体验

## 🔧 发布配置说明

### .csproj 配置

项目文件中的关键配置：

```xml
<!-- 发布配置 -->
<PropertyGroup Condition="'$(Configuration)' == 'Release'">
  <PublishSingleFile>true</PublishSingleFile>              <!-- 单文件发布 -->
  <SelfContained>true</SelfContained>                      <!-- 自包含运行时 -->
  <PublishTrimmed>false</PublishTrimmed>                   <!-- 不裁剪（避免问题）-->
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>  <!-- 包含原生库 -->
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>  <!-- 启用压缩 -->
</PropertyGroup>
```

### Inno Setup 配置

`setup.iss` 中的关键配置：

```ini
; 应用信息
AppName=RecordTime
AppVersion=1.0.0

; 安装路径
DefaultDirName={autopf}\RecordTime

; 权限要求
PrivilegesRequired=admin

; 架构
ArchitecturesAllowed=x64
```

## 📝 版本管理

### 修改版本号

1. **修改 .csproj 文件**（主要）:
   ```xml
   <Version>1.0.0</Version>
   <AssemblyVersion>1.0.0.0</AssemblyVersion>
   <FileVersion>1.0.0.0</FileVersion>
   ```

2. **修改 setup.iss 文件**:
   ```ini
   #define MyAppVersion "1.0.0"
   ```

3. **使用脚本参数**（临时覆盖）:
   ```powershell
   .\build\publish.ps1 -Version "1.0.1"
   ```

## 🐛 常见问题

### 问题 1: 发布失败，提示 DLL 被占用

**原因**: RecordTime 正在运行，DLL 文件被锁定

**解决**:
```powershell
# 方法 1: 手动关闭应用
# 在系统托盘右键退出

# 方法 2: 强制结束进程
taskkill /F /IM RecordTime.exe
```

### 问题 2: Inno Setup 找不到

**原因**: Inno Setup 未安装或路径不正确

**解决**:
```powershell
# 下载安装 Inno Setup
# https://jrsoftware.org/isdl.php

# 或指定自定义路径
.\build\create-installer.ps1 -InnoSetupPath "D:\Tools\InnoSetup\ISCC.exe"
```

### 问题 3: 发布文件过大

**原因**: 自包含运行时包含完整 .NET Framework

**说明**: 这是正常的，优点是用户无需安装 .NET

**优化**（可选）:
- 启用 `PublishTrimmed=true`（可能导致问题）
- 考虑非自包含发布（需要用户安装 .NET Runtime）

### 问题 4: 安装后无法启动

**检查**:
1. 是否有管理员权限
2. Windows Defender 是否拦截
3. 查看日志文件: `%LOCALAPPDATA%\RecordTime\logs\`

## 📤 分发给用户

### 推荐方式

**普通用户**:
- 提供 `RecordTime-v1.0.0-Setup.exe`
- 双击安装，一路下一步

**技术用户**:
- 提供 `RecordTime-v1.0.0-Portable.zip`
- 解压即用，无需安装

### 发布清单

发布前检查：
- [ ] 测试安装版能否正常安装和启动
- [ ] 测试便携版能否正常运行
- [ ] 测试卸载程序能否正常卸载
- [ ] 验证桌面快捷方式和开始菜单
- [ ] 验证应用图标显示正确
- [ ] 测试开机自启动功能
- [ ] 检查版本号是否正确

## 🔄 更新流程

发布新版本时：

1. 更新版本号（见上文"版本管理"）
2. 更新 CHANGELOG.md
3. 运行测试
4. 发布应用程序
5. 创建安装程序
6. 测试安装和升级
7. 发布到 GitHub Releases
8. 通知用户

## 📚 相关文档

- [项目 README](../README.md)
- [开发文档](../CLAUDE.md)
- [Inno Setup 文档](https://jrsoftware.org/ishelp/)
- [.NET 发布文档](https://docs.microsoft.com/dotnet/core/deploying/)
