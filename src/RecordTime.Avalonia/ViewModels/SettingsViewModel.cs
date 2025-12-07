using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RecordTime.Data;
using RecordTime.Core.Services;
using RecordTime.Avalonia.Resources.Strings;
using RecordTime.Avalonia.Services;
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
    private readonly TrayIconService _trayService;

    [ObservableProperty]
    private bool _autoStart = false;

    // 当 AutoStart 属性变化时，更新注册表
    partial void OnAutoStartChanged(bool value)
    {
        // 避免初始化时触发
        if (_trayService == null) return;

        // 同步到托盘服务（会更新注册表）
        if (_trayService.AutoStartEnabled != value)
        {
            _trayService.AutoStartEnabled = value;

            // 保存到配置文件
            _ = _settingsService.UpdateSettingsAsync(s => s.General.AutoStart = value);

            StatusText = value ? StringResources.Current.AutoStartEnabled : StringResources.Current.AutoStartDisabled;
            Log.Information("开机自启动已{Status}", value ? "启用" : "禁用");
        }
    }

    [ObservableProperty]
    private bool _minimizeToTray = true;

    [ObservableProperty]
    private bool _showNotifications = true;

    [ObservableProperty]
    private int _idleTimeoutMinutes = 5;

    [ObservableProperty]
    private string _statusText = StringResources.Current.Ready;

    [ObservableProperty]
    private string? _databasePath;

    [ObservableProperty]
    private string? _databaseSize;

    [ObservableProperty]
    private int _totalSessions;

    [ObservableProperty]
    private string _selectedLanguage = "zh-CN";

    [ObservableProperty]
    private LanguageOption? _selectedLanguageOption;

    // 主题相关属性
    [ObservableProperty]
    private int _selectedThemeIndex = 2; // 默认 Auto (索引 2)

    // 语言选项
    public List<LanguageOption> LanguageOptions { get; } = new()
    {
        new LanguageOption { Code = "zh-CN", Name = "简体中文" },
        new LanguageOption { Code = "en-US", Name = "English" }
    };

    // 主题选项
    public List<ThemeOption> ThemeOptions { get; } = new()
    {
        new ThemeOption { Mode = ThemeService.ThemeMode.Light, NameZh = "浅色", NameEn = "Light" },
        new ThemeOption { Mode = ThemeService.ThemeMode.Dark, NameZh = "深色", NameEn = "Dark" },
        new ThemeOption { Mode = ThemeService.ThemeMode.Auto, NameZh = "跟随系统", NameEn = "System" }
    };

    public SettingsViewModel()
    {
        _settingsService = new AppSettingsService();
        _trayService = TrayIconService.Instance;

        // 订阅托盘服务的 AutoStart 变化
        _trayService.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(TrayIconService.AutoStartEnabled))
            {
                AutoStart = _trayService.AutoStartEnabled;
            }
        };

        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // 加载应用设置
            var settings = _settingsService.GetSettings();

            // AutoStart 从托盘服务读取(托盘服务从注册表读取)
            AutoStart = _trayService.AutoStartEnabled;
            MinimizeToTray = settings.General.MinimizeToTray;
            ShowNotifications = settings.General.ShowNotifications;
            SelectedLanguage = settings.General.Language;
            IdleTimeoutMinutes = settings.Monitoring.IdleTimeoutMinutes;

            // 加载主题设置
            var themeMode = ThemeService.Instance.CurrentMode;
            SelectedThemeIndex = themeMode switch
            {
                ThemeService.ThemeMode.Light => 0,
                ThemeService.ThemeMode.Dark => 1,
                ThemeService.ThemeMode.Auto => 2,
                _ => 2
            };

            // 设置选中的语言选项
            SelectedLanguageOption = LanguageOptions.FirstOrDefault(l => l.Code == SelectedLanguage)
                                    ?? LanguageOptions[0];

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

            StatusText = StringResources.Current.SettingsLoadSuccess;
            Log.Information("设置已加载");
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.SettingsLoadFailed}:{ex.Message}";
            Log.Error(ex, "加载设置失败");
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        // 切换语言
        StringResources.Current.SwitchLanguage(value);

        // 保存到配置
        _ = SaveLanguageSettingAsync(value);

        StatusText = value == "zh-CN" ? StringResources.Current.LanguageSwitchedToChinese : StringResources.Current.LanguageSwitchedToEnglish;
        Log.Information("语言已切换: {Language}", value);
    }

    partial void OnSelectedLanguageOptionChanged(LanguageOption? value)
    {
        if (value != null && value.Code != SelectedLanguage)
        {
            SelectedLanguage = value.Code;
        }
    }

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (value >= 0 && value < ThemeOptions.Count)
        {
            var themeOption = ThemeOptions[value];
            ThemeService.Instance.SetTheme(themeOption.Mode);

            var themeName = SelectedLanguage == "zh-CN" ? themeOption.NameZh : themeOption.NameEn;
            StatusText = string.Format(StringResources.Current.ThemeSwitched, themeName);
            Log.Information("主题已切换: {Theme}", themeOption.Mode);
        }
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
    private async Task ToggleMinimizeToTrayAsync()
    {
        MinimizeToTray = !MinimizeToTray;
        await _settingsService.UpdateSettingsAsync(s => s.General.MinimizeToTray = MinimizeToTray);
        StatusText = StringResources.Current.SettingsSaved;
        Log.Information("最小化到托盘已{Status}", MinimizeToTray ? "启用" : "禁用");
    }

    [RelayCommand]
    private async Task ToggleShowNotificationsAsync()
    {
        ShowNotifications = !ShowNotifications;
        await _settingsService.UpdateSettingsAsync(s => s.General.ShowNotifications = ShowNotifications);
        StatusText = StringResources.Current.SettingsSaved;
        Log.Information("显示通知已{Status}", ShowNotifications ? "启用" : "禁用");
    }

    [RelayCommand]
    private async Task SaveIdleTimeoutAsync()
    {
        await _settingsService.UpdateSettingsAsync(s => s.Monitoring.IdleTimeoutMinutes = IdleTimeoutMinutes);
        StatusText = string.Format(StringResources.Current.IdleTimeoutSet, IdleTimeoutMinutes);
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
            StatusText = StringResources.Current.DatabaseFolderOpened;
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.OpenFailed}:{ex.Message}";
            Log.Error(ex, "打开数据库文件夹失败");
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            StatusText = StringResources.Current.BackingUpDatabase;

            var dbPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "RecordTime",
                "recordtime.db");

            if (!File.Exists(dbPath))
            {
                StatusText = StringResources.Current.DatabaseFileNotExists;
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

            StatusText = $"{StringResources.Current.BackupSuccess}:{Path.GetFileName(backupPath)}";
            Log.Information("数据库备份成功: {BackupPath}", backupPath);
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.BackupFailed}:{ex.Message}";
            Log.Error(ex, "备份数据库失败");
        }
    }

    [RelayCommand]
    private async Task ClearOldDataAsync()
    {
        try
        {
            StatusText = StringResources.Current.ClearingOldData;

            await using var dbContext = new RecordTimeDbContext();
            var thirtyDaysAgo = DateTime.Now.AddDays(-30);

            // 删除 30 天前的数据
            var oldSessions = await dbContext.Sessions
                .Where(s => s.StartTime < thirtyDaysAgo)
                .ToListAsync();

            var deletedCount = oldSessions.Count;
            dbContext.Sessions.RemoveRange(oldSessions);
            await dbContext.SaveChangesAsync();

            StatusText = string.Format(StringResources.Current.OldDataCleared, deletedCount);
            Log.Information("已清除 {Count} 条旧数据", deletedCount);

            await LoadSettingsAsync(); // 重新加载统计
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.ClearFailed}:{ex.Message}";
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
            StatusText = StringResources.Current.LogsFolderOpened;
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.OpenFailed}:{ex.Message}";
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

// 主题选项模型
public class ThemeOption
{
    public ThemeService.ThemeMode Mode { get; set; }
    public string NameZh { get; set; } = string.Empty;
    public string NameEn { get; set; } = string.Empty;
}
