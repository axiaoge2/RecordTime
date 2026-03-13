using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RecordTime.Avalonia.ViewModels;
using Serilog;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 系统托盘图标服务
/// 管理托盘图标行为和命令
/// </summary>
public partial class TrayIconService : ObservableObject
{
    private static TrayIconService? _instance;
    public static TrayIconService Instance => _instance ??= new TrayIconService();

    private MainWindowViewModel? _mainViewModel;
    private Window? _mainWindow;

    [ObservableProperty]
    private bool _autoStartEnabled;

    public TrayIconService()
    {
        // 检查当前自启动状态
        AutoStartEnabled = CheckAutoStartEnabled();
    }

    /// <summary>
    /// 初始化托盘服务
    /// </summary>
    public void Initialize(MainWindowViewModel mainViewModel, Window mainWindow)
    {
        _mainViewModel = mainViewModel;
        _mainWindow = mainWindow;

        // 订阅主窗口的监控状态变化
        _mainViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(MainWindowViewModel.IsMonitoring))
            {
                OnPropertyChanged(nameof(IsMonitoring));
                OnPropertyChanged(nameof(CanStartMonitoring));
                OnPropertyChanged(nameof(CanStopMonitoring));
            }
        };

        Log.Information("托盘图标服务已初始化");
    }

    /// <summary>
    /// 当前是否正在监控
    /// </summary>
    public bool IsMonitoring => _mainViewModel?.IsMonitoring ?? false;

    /// <summary>
    /// 是否可以启动监控
    /// </summary>
    public bool CanStartMonitoring => !IsMonitoring;

    /// <summary>
    /// 是否可以停止监控
    /// </summary>
    public bool CanStopMonitoring => IsMonitoring;

    /// <summary>
    /// 显示主窗口
    /// </summary>
    [RelayCommand]
    private void ShowWindow()
    {
        if (_mainWindow == null) return;

        _mainWindow.Show();
        _mainWindow.WindowState = WindowState.Normal;
        _mainWindow.Activate();

        Log.Debug("主窗口已显示");
    }

    /// <summary>
    /// 启动监控
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartMonitoring))]
    private async Task StartMonitoring()
    {
        if (_mainViewModel == null) return;

        await _mainViewModel.ToggleMonitoringCommand.ExecuteAsync(null);
        Log.Information("从托盘菜单启动专注");
    }

    /// <summary>
    /// 停止监控
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopMonitoring))]
    private async Task StopMonitoring()
    {
        if (_mainViewModel == null) return;

        await _mainViewModel.ToggleMonitoringCommand.ExecuteAsync(null);
        Log.Information("从托盘菜单停止专注");
    }

    /// <summary>
    /// 退出应用程序
    /// </summary>
    [RelayCommand]
    private void Exit()
    {
        Log.Information("从托盘菜单退出应用");

        // 清理资源
        _mainViewModel?.Dispose();

        // 退出应用
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    /// <summary>
    /// 当自启动设置变化时
    /// </summary>
    partial void OnAutoStartEnabledChanged(bool value)
    {
        var success = SetAutoStart(value);
        if (success)
        {
            Log.Information("开机自启动设置已更改为: {Enabled}", value);
        }
        else
        {
            // 如果设置失败，恢复原状态
            Log.Warning("开机自启动设置失败，恢复原状态");
            AutoStartEnabled = !value;
        }
    }

    /// <summary>
    /// 检查是否已启用开机自启动
    /// </summary>
    private bool CheckAutoStartEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", false);

            if (key == null) return false;

            var value = key.GetValue("RecordTime");
            var isEnabled = value != null;
            Log.Debug("检查自启动状态: {Enabled}", isEnabled);
            return isEnabled;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "检查自启动状态失败");
            return false;
        }
    }

    /// <summary>
    /// 设置开机自启动
    /// </summary>
    /// <returns>是否成功设置</returns>
    private bool SetAutoStart(bool enable)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key == null)
            {
                Log.Error("无法打开注册表键 (需要写入权限)");
                return false;
            }

            if (enable)
            {
                // 获取当前可执行文件路径
                var exePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    Log.Error("无法获取当前可执行文件路径");
                    return false;
                }

                key.SetValue("RecordTime", $"\"{exePath}\"");
                Log.Information("已添加开机自启动: {ExePath}", exePath);

                // 验证是否成功写入
                var verifyValue = key.GetValue("RecordTime") as string;
                if (verifyValue == null || !verifyValue.Contains(exePath))
                {
                    Log.Error("注册表写入验证失败");
                    return false;
                }

                Log.Information("开机自启动设置成功！路径: {ExePath}", exePath);
                Log.Information("注意: Windows 可能会显示'已禁止开机自启动'通知，这是 Windows 的安全提示。");
                Log.Information("您可以在 Windows 设置 > 应用 > 启动 中手动启用 RecordTime。");
            }
            else
            {
                key.DeleteValue("RecordTime", false);
                Log.Information("已移除开机自启动");
            }

            return true;
        }
        catch (UnauthorizedAccessException ex)
        {
            Log.Error(ex, "设置自启动失败: 没有权限修改注册表");
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "设置自启动失败");
            return false;
        }
    }
}
