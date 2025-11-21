using System;
using System.Diagnostics;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Avalonia.Resources.Strings;

namespace RecordTime.Avalonia.ViewModels;

public partial class AboutViewModel : ViewModelBase
{
    public string AppName => "RecordTime";
    public string Version => "1.0.0";
    public string Description => "RecordTime 是一款智能时间追踪与效率分析工具。自动记录应用使用时长，结合 AI 智能分析提供个性化洞察，帮助您科学管理时间、提升工作效率。所有数据本地存储，保护您的隐私安全。";
    public string Copyright => $"© {DateTime.Now.Year} RecordTime. All rights reserved.";

    // 核心特色功能 - 支持多语言
    public string[] CoreFeatures => new[]
    {
        StringResources.Current.CoreFeature1,
        StringResources.Current.CoreFeature2,
        StringResources.Current.CoreFeature3,
        StringResources.Current.CoreFeature4
    };

    // 数据安全保障 - 支持多语言
    public string[] SecurityFeatures => new[]
    {
        StringResources.Current.SecurityFeature1,
        StringResources.Current.SecurityFeature2,
        StringResources.Current.SecurityFeature3,
        StringResources.Current.SecurityFeature4
    };

    // 用户体验优化 - 支持多语言
    public string[] UXFeatures => new[]
    {
        StringResources.Current.UXFeature1,
        StringResources.Current.UXFeature2,
        StringResources.Current.UXFeature3,
        StringResources.Current.UXFeature4
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
