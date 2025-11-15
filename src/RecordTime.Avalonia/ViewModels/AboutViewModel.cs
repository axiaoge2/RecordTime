using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RecordTime.Avalonia.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    public string AppName => "RecordTime";
    public string Version => "1.0.0";
    public string Description => "RecordTime 是一款 Windows 桌面时间追踪工具,帮助您了解时间的使用情况,提升工作效率。";
    public string Copyright => $"© {DateTime.Now.Year} RecordTime. All rights reserved.";

    public string[] Features => new[]
    {
        "✓ 实时监控应用使用情况",
        "✓ 详细的时间使用统计",
        "✓ 美观的数据可视化图表",
        "✓ HTML 格式报告导出",
        "✓ AI 智能分析建议",
        "✓ 开机自动启动",
        "✓ 系统托盘最小化",
        "✓ 数据库备份与管理"
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
