# RecordTime 技术文档索引

本目录包含 RecordTime 项目的技术文档、架构设计和问题解决方案。

## 文档列表

### 架构与设计

- **[ARCHITECTURE.md](./ARCHITECTURE.md)** - 系统架构设计文档
  - 项目整体架构
  - 技术栈说明
  - 模块划分

- **[AI_INTEGRATION.md](./AI_INTEGRATION.md)** - AI 集成文档
  - AI 功能集成指南
  - API 使用说明

### 问题解决方案

- **[ScrollViewer-Padding-Issue.md](./ScrollViewer-Padding-Issue.md)** ⭐ **重要** - Avalonia ScrollViewer Padding 滚动问题完整解决方案
  - **问题:** ScrollViewer 设置 Padding 导致底部内容无法滚动到视图中
  - **根本原因:** Avalonia 布局引擎不将 ScrollViewer 的 Padding 计入可滚动范围
  - **解决方案:** 将 Padding 移至子元素的 Margin,底部 Margin 加倍,添加透明占位元素
  - **影响范围:** 仪表盘页面、应用统计页面
  - **创建日期:** 2025-11-15
  - **记忆口诀:** "ScrollViewer 不要 Padding,子元素 Margin 底部加倍"

- **[TOP_APPS_SCROLL_FIX.md](./TOP_APPS_SCROLL_FIX.md)** ⚠️ *已过时* - TOP 10 应用列表滚动修复(临时文档)
  - 注: 此文档为早期临时记录,完整解决方案请参考上方的 `ScrollViewer-Padding-Issue.md`

## 快速查找

### 按问题类型

**滚动问题:**
- [ScrollViewer-Padding-Issue.md](./ScrollViewer-Padding-Issue.md) - ScrollViewer Padding 导致滚动范围计算错误

**UI/布局问题:**
- [ScrollViewer-Padding-Issue.md](./ScrollViewer-Padding-Issue.md) - 底部内容被截断无法查看

**Avalonia 特定问题:**
- [ScrollViewer-Padding-Issue.md](./ScrollViewer-Padding-Issue.md) - Avalonia ScrollViewer 布局机制

### 按修复日期

- **2025-11-15** - [ScrollViewer-Padding-Issue.md](./ScrollViewer-Padding-Issue.md)
- **2025-11-14** - [TOP_APPS_SCROLL_FIX.md](./TOP_APPS_SCROLL_FIX.md) (已过时)

## 文档编写规范

### 问题解决方案文档格式

每个问题解决方案文档应包含:

1. **问题描述** - 清晰描述问题症状
2. **根本原因** - 深入分析问题的技术根源
3. **解决方案** - 详细的修复步骤和代码示例
4. **实际案例** - 项目中的真实修复案例
5. **最佳实践** - 总结的最佳实践建议
6. **诊断工具** - 如何快速判断和调试类似问题

### Markdown 格式要求

- 使用清晰的标题层级
- 代码块必须指定语言(xml, csharp, bash 等)
- 使用 ✅ ❌ 等符号标注正确/错误的做法
- 提供前后对比代码示例
- 包含文档版本和日期信息

## 贡献指南

当你遇到新的技术问题并解决后,请:

1. 创建新的 Markdown 文档描述问题和解决方案
2. 更新本 README.md 添加文档索引
3. 在相关代码中添加注释引用文档

## 维护记录

- **2025-11-15** - 创建文档索引,添加 ScrollViewer Padding 问题完整文档
- **2025-11-14** - 初始化 docs 目录,添加架构文档

---

**项目:** RecordTime - 时间追踪工具
**文档维护者:** Claude Code
**最后更新:** 2025-11-15
