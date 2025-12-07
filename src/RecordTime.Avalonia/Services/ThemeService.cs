using Avalonia;
using Avalonia.Styling;
using RecordTime.Core.Services;
using Serilog;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Kernel;
using RecordTime.Core.Models;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 主题服务 - 管理应用程序的主题切换
/// </summary>
public class ThemeService : INotifyPropertyChanged
{
    private static ThemeService? _instance;
    public static ThemeService Instance => _instance ??= new ThemeService();

    private readonly AppSettingsService _settingsService;

    /// <summary>
    /// 主题模式枚举
    /// </summary>
    public enum ThemeMode
    {
        Light,
        Dark,
        Auto
    }

    private ThemeMode _currentMode = ThemeMode.Auto;
    /// <summary>
    /// 当前主题模式
    /// </summary>
    public ThemeMode CurrentMode
    {
        get => _currentMode;
        private set
        {
            if (_currentMode != value)
            {
                _currentMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsLightTheme));
                OnPropertyChanged(nameof(IsDarkTheme));
                OnPropertyChanged(nameof(IsAutoTheme));
            }
        }
    }

    /// <summary>
    /// 当前是否为浅色主题
    /// </summary>
    public bool IsLightTheme => CurrentMode == ThemeMode.Light;

    /// <summary>
    /// 当前是否为深色主题
    /// </summary>
    public bool IsDarkTheme => CurrentMode == ThemeMode.Dark;

    /// <summary>
    /// 当前是否为自动主题
    /// </summary>
    public bool IsAutoTheme => CurrentMode == ThemeMode.Auto;

    /// <summary>
    /// 实际应用的主题是否为深色
    /// </summary>
    public bool ActualThemeIsDark
    {
        get
        {
            var app = Application.Current;
            if (app == null) return false;
            return app.ActualThemeVariant == ThemeVariant.Dark;
        }
    }

    /// <summary>
    /// 主题变更事件
    /// </summary>
    public event EventHandler<ThemeMode>? ThemeChanged;

    private ThemeService()
    {
        _settingsService = new AppSettingsService();
    }

    /// <summary>
    /// 初始化主题服务
    /// </summary>
    public void Initialize()
    {
        try
        {
            var settings = _settingsService.GetSettings();
            var mode = ParseThemeMode(settings.UI.Theme);
            SetTheme(mode, saveSettings: false);

            // 监听系统主题变化
            if (Application.Current?.PlatformSettings != null)
            {
                Application.Current.PlatformSettings.ColorValuesChanged += OnSystemThemeChanged;
            }

            Log.Information("主题服务已初始化: {Theme}", mode);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "初始化主题服务失败，使用默认主题");
            SetTheme(ThemeMode.Auto, saveSettings: false);
        }
    }

    /// <summary>
    /// 设置主题
    /// </summary>
    /// <param name="mode">主题模式</param>
    /// <param name="saveSettings">是否保存到设置</param>
    public void SetTheme(ThemeMode mode, bool saveSettings = true)
    {
        CurrentMode = mode;
        var app = Application.Current;

        if (app == null) return;

        ThemeVariant targetTheme = mode switch
        {
            ThemeMode.Light => ThemeVariant.Light,
            ThemeMode.Dark => ThemeVariant.Dark,
            ThemeMode.Auto => GetSystemTheme(),
            _ => ThemeVariant.Light
        };

        app.RequestedThemeVariant = targetTheme;

        // 更新 LiveCharts 主题
        ConfigureLiveChartsTheme(targetTheme == ThemeVariant.Dark);

        if (saveSettings)
        {
            SaveThemeSetting(mode);
        }

        // 触发主题变更事件
        ThemeChanged?.Invoke(this, mode);

        Log.Information("主题已切换: {Mode} -> {ActualTheme}",
            mode, targetTheme);
    }

    /// <summary>
    /// 切换到浅色主题
    /// </summary>
    public void SetLightTheme() => SetTheme(ThemeMode.Light);

    /// <summary>
    /// 切换到深色主题
    /// </summary>
    public void SetDarkTheme() => SetTheme(ThemeMode.Dark);

    /// <summary>
    /// 切换到自动主题
    /// </summary>
    public void SetAutoTheme() => SetTheme(ThemeMode.Auto);

    /// <summary>
    /// 切换主题 (Light -> Dark -> Auto -> Light)
    /// </summary>
    public void ToggleTheme()
    {
        var nextMode = CurrentMode switch
        {
            ThemeMode.Light => ThemeMode.Dark,
            ThemeMode.Dark => ThemeMode.Auto,
            ThemeMode.Auto => ThemeMode.Light,
            _ => ThemeMode.Light
        };
        SetTheme(nextMode);
    }

    /// <summary>
    /// 获取系统主题
    /// </summary>
    private ThemeVariant GetSystemTheme()
    {
        var platformSettings = Application.Current?.PlatformSettings;
        if (platformSettings != null)
        {
            var colorValues = platformSettings.GetColorValues();
            // PlatformThemeVariant 需要转换为 ThemeVariant
            return colorValues.ThemeVariant == global::Avalonia.Platform.PlatformThemeVariant.Dark
                ? ThemeVariant.Dark
                : ThemeVariant.Light;
        }
        return ThemeVariant.Light;
    }

    /// <summary>
    /// 系统主题变化处理
    /// </summary>
    private void OnSystemThemeChanged(object? sender, global::Avalonia.Platform.PlatformColorValues e)
    {
        if (CurrentMode == ThemeMode.Auto)
        {
            var app = Application.Current;
            if (app != null)
            {
                // PlatformThemeVariant 需要转换为 ThemeVariant
                var themeVariant = e.ThemeVariant == global::Avalonia.Platform.PlatformThemeVariant.Dark
                    ? ThemeVariant.Dark
                    : ThemeVariant.Light;
                app.RequestedThemeVariant = themeVariant;
                ConfigureLiveChartsTheme(themeVariant == ThemeVariant.Dark);

                // 通知属性变更
                OnPropertyChanged(nameof(ActualThemeIsDark));

                Log.Debug("系统主题已变化: {Theme}", themeVariant);
            }
        }
    }

    /// <summary>
    /// 配置 LiveCharts 主题
    /// </summary>
    private void ConfigureLiveChartsTheme(bool isDark)
    {
        try
        {
            LiveCharts.Configure(config =>
            {
                config.AddSkiaSharp();
                config.AddDefaultMappers();

                if (isDark)
                {
                    config.AddDarkTheme();
                }
                else
                {
                    config.AddLightTheme();
                }

                // 保持 DailyUsage 类型的自定义映射
                config.HasMap<DailyUsage>((dailyUsage, index) => new Coordinate(index, dailyUsage.TotalHours));
            });

            Log.Debug("LiveCharts 主题已配置: {Theme}", isDark ? "Dark" : "Light");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "配置 LiveCharts 主题失败");
        }
    }

    /// <summary>
    /// 解析主题模式字符串
    /// </summary>
    private static ThemeMode ParseThemeMode(string? theme)
    {
        return theme?.ToLowerInvariant() switch
        {
            "light" => ThemeMode.Light,
            "dark" => ThemeMode.Dark,
            "auto" or "system" => ThemeMode.Auto,
            _ => ThemeMode.Auto
        };
    }

    /// <summary>
    /// 保存主题设置
    /// </summary>
    private void SaveThemeSetting(ThemeMode mode)
    {
        try
        {
            var settings = _settingsService.GetSettings();
            settings.UI.Theme = mode.ToString();
            // 使用异步方法但不等待结果（fire and forget）
            _ = _settingsService.SaveSettingsAsync(settings);
            Log.Debug("主题设置已保存: {Theme}", mode);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "保存主题设置失败");
        }
    }

    #region INotifyPropertyChanged

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    #endregion
}
