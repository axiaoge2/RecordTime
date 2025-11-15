# VS Code 快速开始指南

## 🎉 恭喜！项目已配置完成

你现在可以在VS Code中开发RecordTime项目了。我已经为你创建了一个**控制台测试程序**，可以立即运行和测试窗口监控功能。

---

## 🚀 立即运行测试程序

### 方法1: 使用VS Code终端（推荐）

1. **打开VS Code**
```bash
code E:\recordtime
```

2. **打开终端** (Ctrl + `)

3. **运行测试程序**
```bash
dotnet run --project src/RecordTime.Console
```

### 方法2: 使用命令行

```bash
cd E:\recordtime
dotnet run --project src/RecordTime.Console
```

---

## 📺 测试程序功能演示

运行后你会看到：

```
╔════════════════════════════════════════════╗
║   RecordTime - 窗口监控测试程序           ║
╚════════════════════════════════════════════╝

📊 初始化数据库...
✅ 数据库就绪

🚀 开始监控窗口活动...
💡 提示: 切换不同的应用窗口，观察监控效果
⚠️  按 Ctrl+C 停止监控

════════════════════════════════════════════════════════════

⏰ 14:30:25
📌 应用: chrome
🏷️  分类: 浏览器
🎯 活动: PassiveBrowsing
📝 标题: RecordTime项目 - GitHub
────────────────────────────────────────────────────────────

⏰ 14:30:35
📌 应用: Code
🏷️  分类: 开发工具
🎯 活动: ActiveTyping
📝 标题: Program.cs - RecordTime
────────────────────────────────────────────────────────────

📊 今日统计 (14:31:00)
   总时长: 00:00:35
   会话数: 2
```

**实时功能**:
- ✅ 自动检测窗口切换
- ✅ 识别应用类型（浏览器、开发工具、办公软件等）
- ✅ 判定活动类型（主动输入、被动浏览等）
- ✅ 保存到SQLite数据库
- ✅ 每30秒显示统计数据

---

## 🎯 测试建议

运行测试程序后，尝试：

1. **切换到浏览器** → 应显示为"浏览器"分类
2. **切换到VS Code** → 应显示为"开发工具"分类
3. **打开视频播放器** (如VLC) → 应识别为"视频娱乐"
4. **等待30秒** → 查看统计信息更新

---

## 📁 项目结构

```
E:\recordtime\
├── src/
│   ├── RecordTime.Core/       ✅ 核心监控逻辑 (可编译)
│   ├── RecordTime.Data/       ✅ 数据访问层 (可编译)
│   ├── RecordTime.Console/    ✅ 测试程序 (可运行) 👈 新增
│   └── RecordTime.UI/         ⚠️  WinUI 3界面 (需要VS 2022)
├── docs/                      📚 技术文档
├── README.md
└── VSCODE_QUICK_START.md     📖 本文件
```

---

## 🛠️ VS Code开发工作流

### 1. 打开项目
```bash
code E:\recordtime
```

### 2. 推荐的VS Code扩展

确保已安装（在扩展市场搜索）：
- ✅ **C#** (Microsoft官方)
- ✅ **C# Dev Kit**
- ✅ **NuGet Package Manager**
- ✅ **SQLite Viewer** (可选，查看数据库)

### 3. 常用命令

在VS Code终端 (Ctrl + `) 中运行：

```bash
# 编译所有项目
dotnet build

# 编译指定项目
dotnet build src/RecordTime.Core

# 运行测试程序
dotnet run --project src/RecordTime.Console

# 清理编译文件
dotnet clean

# 还原NuGet包
dotnet restore
```

### 4. 调试配置

VS Code会自动识别.NET项目。按 `F5` 启动调试，或：

1. 点击左侧"运行和调试"图标
2. 选择"RecordTime.Console"
3. 点击绿色播放按钮

---

## 📊 查看数据库

### 方法1: 使用SQLite浏览器

1. 安装 **DB Browser for SQLite**
   - 下载: https://sqlitebrowser.org/

2. 打开数据库文件:
   ```
   %LOCALAPPDATA%\RecordTime\recordtime.db
   完整路径: C:\Users\你的用户名\AppData\Local\RecordTime\recordtime.db
   ```

### 方法2: 使用VS Code扩展

1. 安装 **SQLite Viewer** 扩展
2. 在VS Code中右键数据库文件 → "Open Database"

---

## 🎓 接下来可以做什么？

### 初级任务 (VS Code可完成)

1. **修改活动检测逻辑**
   - 文件: `src/RecordTime.Core/Services/ActivityDetector.cs`
   - 尝试: 添加更多应用到分类列表

2. **优化窗口监控**
   - 文件: `src/RecordTime.Core/Services/WindowMonitor.cs`
   - 尝试: 修改监控间隔（当前2秒）

3. **扩展测试程序**
   - 文件: `src/RecordTime.Console/Program.cs`
   - 尝试: 添加更多统计信息显示

### 中级任务 (需要学习Win32 API)

4. **实现InputMonitor**
   - 创建: `src/RecordTime.Core/Services/InputMonitor.cs`
   - 功能: 监控键盘鼠标活动
   - 参考: `docs/ARCHITECTURE.md` 第2.1节

5. **实现MediaDetector**
   - 创建: `src/RecordTime.Core/Services/MediaDetector.cs`
   - 功能: 检测视频播放状态
   - 参考: `docs/ARCHITECTURE.md` 第2.1节

6. **实现SessionManager**
   - 创建: `src/RecordTime.Core/Services/SessionManager.cs`
   - 功能: 自动管理会话生命周期
   - 参考: `docs/ARCHITECTURE.md` 第2.3节

---

## 💡 开发技巧

### 实时编译（文件保存时自动编译）

```bash
# 在VS Code终端运行
dotnet watch run --project src/RecordTime.Console
```

### 查看详细编译信息

```bash
dotnet build --verbosity detailed
```

### 快速导航代码

- **跳转到定义**: F12
- **查找引用**: Shift+F12
- **重命名符号**: F2
- **格式化代码**: Shift+Alt+F

---

## ❓ 常见问题

### Q: 测试程序没有检测到窗口切换？
A: 确保：
1. 程序正在运行
2. 切换到其他可见窗口（不要最小化）
3. 检查控制台是否有错误信息

### Q: 数据库文件在哪里？
A:
```
Windows: %LOCALAPPDATA%\RecordTime\recordtime.db
快速打开: Win+R → 输入 %LOCALAPPDATA%\RecordTime → 回车
```

### Q: 如何停止测试程序？
A: 在控制台按 `Ctrl+C`

### Q: 编译错误怎么办？
A:
```bash
# 清理后重新编译
dotnet clean
dotnet restore
dotnet build
```

---

## 🔄 同步代码（如果使用Git）

```bash
# 初始化Git仓库
git init

# 添加所有文件
git add .

# 提交
git commit -m "Initial commit - RecordTime .NET 7.0"

# 连接远程仓库（可选）
git remote add origin https://github.com/你的用户名/recordtime.git
git push -u origin main
```

---

## 📞 获取帮助

- **查看架构文档**: `docs/ARCHITECTURE.md`
- **AI分析方案**: `docs/AI_INTEGRATION.md`
- **.NET 7迁移说明**: `NET7_MIGRATION_NOTES.md`

---

**祝你开发顺利！** 🎊

现在就运行 `dotnet run --project src/RecordTime.Console` 看看效果吧！
