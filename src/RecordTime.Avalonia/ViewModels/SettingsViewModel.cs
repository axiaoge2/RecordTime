using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using RecordTime.Data;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace RecordTime.Avalonia.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
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

    public SettingsViewModel()
    {
        _ = LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            // 加载数据库信息
            await using var dbContext = new RecordTimeDbContext();
            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recordtime.db");

            if (File.Exists(dbPath))
            {
                var fileInfo = new FileInfo(dbPath);
                DatabasePath = dbPath;
                DatabaseSize = FormatFileSize(fileInfo.Length);
            }

            // 统计会话数量
            TotalSessions = await dbContext.Sessions.CountAsync();
        }
        catch (Exception ex)
        {
            StatusText = $"加载设置失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void ToggleAutoStart()
    {
        AutoStart = !AutoStart;
        StatusText = AutoStart ? "已启用开机自启动" : "已禁用开机自启动";
        // TODO: 实现注册表写入逻辑
    }

    [RelayCommand]
    private void OpenDatabaseFolder()
    {
        try
        {
            var dbFolder = AppDomain.CurrentDomain.BaseDirectory;
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
            StatusText = $"打开失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private async Task BackupDatabaseAsync()
    {
        try
        {
            StatusText = "正在备份数据库...";

            var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "recordtime.db");
            if (!File.Exists(dbPath))
            {
                StatusText = "数据库文件不存在";
                return;
            }

            var backupFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "backups");
            Directory.CreateDirectory(backupFolder);

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var backupPath = Path.Combine(backupFolder, $"recordtime_backup_{timestamp}.db");

            await Task.Run(() => File.Copy(dbPath, backupPath));

            StatusText = $"备份成功：{Path.GetFileName(backupPath)}";
        }
        catch (Exception ex)
        {
            StatusText = $"备份失败：{ex.Message}";
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
            var oldSessions = dbContext.Sessions.Where(s => s.StartTime < thirtyDaysAgo);
            dbContext.Sessions.RemoveRange(oldSessions);

            var deletedCount = await dbContext.SaveChangesAsync();

            StatusText = $"已清除 {deletedCount} 条旧数据";
            await LoadSettingsAsync(); // 重新加载统计
        }
        catch (Exception ex)
        {
            StatusText = $"清除失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenLogsFolder()
    {
        try
        {
            var logsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");

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
            StatusText = $"打开失败：{ex.Message}";
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
