# 贡献指南

感谢你对 RecordTime 项目的关注！我们欢迎各种形式的贡献。

## 如何贡献

### 报告 Bug

1. 在 [Issues](https://github.com/axiaoge2/recordtime/issues) 中搜索是否已有相似问题
2. 如果没有，创建新的 Issue，使用 Bug Report 模板
3. 提供详细的复现步骤、系统环境和错误日志

### 功能建议

1. 在 [Issues](https://github.com/axiaoge2/recordtime/issues) 中创建 Feature Request
2. 描述你希望的功能和使用场景
3. 如果可能，提供设计思路或实现建议

### 提交代码

1. Fork 本仓库
2. 创建你的特性分支：`git checkout -b feature/AmazingFeature`
3. 提交你的修改：`git commit -m 'feat: add some amazing feature'`
4. 推送到分支：`git push origin feature/AmazingFeature`
5. 创建 Pull Request

## 开发环境设置

### 前置要求

- .NET 7.0 SDK
- Visual Studio 2022 / VS Code / JetBrains Rider
- Windows 10/11（开发和测试）

### 克隆和构建

```bash
# 克隆仓库
git clone https://github.com/axiaoge2/recordtime.git
cd recordtime

# 还原依赖
dotnet restore

# 构建项目
dotnet build

# 运行应用
dotnet run --project src/RecordTime.Avalonia
```

### 数据库迁移

```bash
# 添加迁移
dotnet ef migrations add MigrationName \
    --project src/RecordTime.Data \
    --startup-project src/RecordTime.Avalonia

# 应用迁移
dotnet ef database update \
    --project src/RecordTime.Data \
    --startup-project src/RecordTime.Avalonia
```

## 代码规范

### C# 编码规范

- 遵循 [Microsoft C# 编码规范](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- 使用 4 空格缩进
- 类名使用 PascalCase
- 私有字段使用 `_camelCase`
- 方法和属性使用 PascalCase

### 提交信息格式

使用 [Conventional Commits](https://www.conventionalcommits.org/) 规范：

```
<type>: <description>

[optional body]

[optional footer]
```

**类型 (type)**：
- `feat`: 新功能
- `fix`: Bug 修复
- `docs`: 文档更新
- `style`: 代码格式（不影响代码运行的变动）
- `refactor`: 重构（既不是新增功能，也不是修复 bug）
- `perf`: 性能优化
- `test`: 测试相关
- `chore`: 构建过程或辅助工具的变动

**示例**：
```
feat: add time budget reminder notifications

Add NotificationService to send desktop notifications
when users approach their time budget limits.

Closes #123
```

### 代码格式化

提交前请运行：

```bash
dotnet format
```

## Pull Request 流程

1. 确保你的代码通过所有测试
2. 更新相关文档（README、ARCHITECTURE.md 等）
3. 在 PR 描述中说明：
   - 修改了什么
   - 为什么要修改
   - 如何测试
4. 关联相关的 Issue（如果有）
5. 等待代码审查

## 项目结构

```
src/
├── RecordTime.Avalonia/    # UI 层 (Avalonia + MVVM)
├── RecordTime.Core/        # 核心业务逻辑
└── RecordTime.Data/        # 数据访问层

tools/                      # 验证和调试工具
docs/                       # 文档
tests/                      # 单元测试（规划中）
```

## 测试

目前项目测试覆盖率有限，欢迎贡献测试用例！

```bash
# 运行测试
dotnet test

# 使用验证工具
dotnet run --project tools/VerifyHeartbeat
dotnet run --project tools/VerifyIndexes
```

## 多语言贡献

如果你想添加新语言支持：

1. 在 `src/RecordTime.Avalonia/Resources/Strings/` 创建新的语言文件
2. 实现 `IStringProvider` 接口
3. 在 `StringResources.SwitchLanguage()` 添加语言代码
4. 测试所有 UI 文本是否正确显示

## 问题？

如果你有任何问题，欢迎在 Issues 中提问或发起 Discussion。

感谢你的贡献！
