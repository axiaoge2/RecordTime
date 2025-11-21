using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RecordTime.Avalonia.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    public string AppName => "RecordTime";
    public string Version => "1.0.0";
    public string Description => "RecordTime 是一款智能时间追踪与效率分析工具。自动记录应用使用时长，结合 AI 智能分析提供个性化洞察，帮助您科学管理时间、提升工作效率。所有数据本地存储，保护您的隐私安全。";
    public string Copyright => $"© {DateTime.Now.Year} RecordTime. All rights reserved.";

    // 核心特色功能
    public string[] CoreFeatures => new[]
    {
        "🎯 智能活动检测 - 自动识别视频、游戏、主动输入、被动浏览等活动类型",
        "📊 可视化统计分析 - 实时图表展示应用使用分布和时间趋势",
        "🤖 AI 智能洞察 - 可选的 AI 分析提供个性化时间管理建议",
        "📈 专业报告生成 - 支持导出详细的 HTML 格式报告,可在浏览器查看"
    };

    // 数据安全保障
    public string[] SecurityFeatures => new[]
    {
        "🔒 本地优先存储 - 所有数据存储在您的设备上,永不上传云端",
        "🔐 隐私数据加密 - 窗口标题使用 SHA256 哈希加密存储",
        "💾 心跳机制保护 - 每30秒自动保存,防止崩溃导致数据丢失",
        "🛡️ 自动修复机制 - 启动时检测并修复异常会话,确保数据完整性"
    };

    // 用户体验优化
    public string[] UXFeatures => new[]
    {
        "🚀 轻量级运行 - 低资源占用,后台静默监控不影响正常使用",
        "🎨 现代化界面 - Apple 风格设计,简洁美观的用户界面",
        "⚡ 高性能优化 - 500ms 窗口轮询,准确捕捉应用切换",
        "🔧 灵活配置 - 支持多 AI 配置、自定义命名、状态持久化"
    };

    public string TechStack => ".NET 7.0 + Avalonia UI 11.3.8 + Entity Framework Core + SQLite";

    [RelayCommand]
    private void OpenGitHub()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/recordtime/recordtime",
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail if browser doesn't open
        }
    }

    [RelayCommand]
    private void OpenLicense()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/recordtime/recordtime/blob/main/LICENSE",
                UseShellExecute = true
            });
        }
        catch
        {
            // Silently fail if browser doesn't open
        }
    }
}
