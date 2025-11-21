using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RecordTime.Data;
using RecordTime.Core.Services;
using RecordTime.Avalonia.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly AppSettingsService _settingsService;

    [ObservableProperty]
    private bool _autoStart = false;

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private int _idleTimeoutMinutes = 5;

    [ObservableProperty]
    private string _statusText = "就绪";

    [ObservableProperty]
    private string? _databasePath;

    [ObservableProperty]
    private string? _databaseSize;

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private string _selectedLanguage = "zh-CN";

    // 语言选项
    public List<LanguageOption> LanguageOptions { get; } = new()
    {
        new LanguageOption { Code = "zh-CN", Name = "简体中文" },
        new LanguageOption { Code = "en-US", Name = "English" }
    };

    public SettingsViewModel()
    {
        _settingsService = new AppSettingsService();
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // 加载应用设置
            var settings = _settingsService.GetSettings();

            AutoStart = settings.General.AutoStart;
            MinimizeToTray = settings.General.MinimizeToTray;
            ShowNotifications = settings.General.ShowNotifications;
            SelectedLanguage = settings.General.Language;
            IdleTimeoutMinutes = settings.Monitoring.IdleTimeoutMinutes;

            // 加载数据库信息
            await using var dbContext = new RecordTimeDbContext();
            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime",
                "recordtime.db");

            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                DatabasePath = dbPath;
                DatabaseSize = FormatFileSize(fileInfo.Length);
            }

            // 统计会话数量
            TotalSessions = await dbContext.Sessions.CountAsync();

            StatusText = "设置加载成功";
            Log.Information("设置已加载");
        }
        catch (Exception ex)
        {
            StatusText = $"加载设置失败:{ex.Message}";
            Log.Error(ex, "加载设置失败");
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        // 切换语言
        StringResources.Current.SwitchLanguage(value);

        // 保存到配置
        _ = SaveLanguageSettingAsync(value);

        StatusText = value == "zh-CN" ? "语言已切换为简体中文" : "Language switched to English";
        Log.Information("语言已切换: {Language}", value);
    }

    private async Task SaveLanguageSettingAsync(string language)
    {
        try
        {
            await _settingsService.UpdateSettingsAsync(s => s.General.Language = language);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存语言设置失败");
        }
    }

    [RelayCommand]
    private async Task ToggleAutoStartAsync()
    {
        AutoStart = !AutoStart;
        await _settingsService.UpdateSettingsAsync(s => s.General.AutoStart = AutoStart);
        StatusText = AutoStart ? "已启用开机自启动" : "已禁用开机自启动";
        Log.Information("开机自启动已{Status}", AutoStart ? "启用" : "禁用");
    }

    [RelayCommand]
    private async Task ToggleMinimizeToTrayAsync()
    {
        MinimizeToTray = !MinimizeToTray;
        await _settingsService.UpdateSettingsAsync(s => s.General.MinimizeToTray = MinimizeToTray);
        StatusText = "设置已保存";
        Log.Information("最小化到托盘已{Status}", MinimizeToTray ? "启用" : "禁用");
    }

    [RelayCommand]
    private async Task ToggleShowNotificationsAsync()
    {
        ShowNotifications = !ShowNotifications;
        await _settingsService.UpdateSettingsAsync(s => s.General.ShowNotifications = ShowNotifications);
        StatusText = "设置已保存";
        Log.Information("显示通知已{Status}", ShowNotifications ? "启用" : "禁用");
    }

    [RelayCommand]
    private async Task SaveIdleTimeoutAsync()
    {
        await _settingsService.UpdateSettingsAsync(s => s.Monitoring.IdleTimeoutMinutes = IdleTimeoutMinutes);
        StatusText = $"空闲超时已设置为 {IdleTimeoutMinutes} 分钟";
        Log.Information("空闲超时已设置为 {Minutes} 分钟", IdleTimeoutMinutes);
    }

    [RelayCommand]
    private void OpenDatabaseFolder()
    {
        try
        {
            var dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime");
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = dbFolder,
                UseShellExecute = true
            });
            StatusText = "已打开数据库文件夹";
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败:{ex.Message}";
            Log.Error(ex, "打开数据库文件夹失败");
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            StatusText = "正在备份数据库...";

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime",
                "recordtime.db");

            if (!File.Exists(dbPath))
            {
                StatusText = "数据库文件不存在";
                return;
            }

            var backupFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime",
                "backups");
            Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupFolder, $"recordtime_backup_{timestamp}.db");

            await Task.Run(() => File.Copy(dbPath, backupPath));

            StatusText = $"备份成功:{Path.GetFileName(backupPath)}";
            Log.Information("数据库备份成功: {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            StatusText = $"备份失败:{ex.Message}";
            Log.Error(ex, "备份数据库失败");
        }
    }

    [RelayCommand]
    private async Task ClearOldDataAsync()
    {
        try
        {
            StatusText = "正在清除旧数据...";

            await using var dbContext = new RecordTimeDbContext();
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            // 删除 30 天前的数据
            var oldSessions = await dbContext.Sessions
                .Where(s => s.StartTime < thirtyDaysAgo)
                .ToListAsync();

            var deletedCount = oldSessions.Count;
            dbContext.Sessions.RemoveRange(oldSessions);
            await dbContext.SaveChangesAsync();

            StatusText = $"已清除 {deletedCount} 条旧数据";
            Log.Information("已清除 {Count} 条旧数据", deletedCount);

            await LoadSettingsAsync(); // 重新加载统计
        }
        catch (Exception ex)
        {
            StatusText = $"清除失败:{ex.Message}";
            Log.Error(ex, "清除旧数据失败");
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            var logsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime",
                "logs");

            if (!Directory.Exists(logsFolder))
            {
                Directory.CreateDirectory(logsFolder);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = logsFolder,
                UseShellExecute = true
            });
            StatusText = "已打开日志文件夹";
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败:{ex.Message}";
            Log.Error(ex, "打开日志文件夹失败");
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;

        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }
}

// 语言选项模型
public class LanguageOption
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
