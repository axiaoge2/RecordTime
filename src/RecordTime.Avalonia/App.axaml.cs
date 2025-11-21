using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Platform.Storage;
using Avalonia.Platform;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Markup.Xaml;
using RecordTime.Avalonia.ViewModels;
using RecordTime.Avalonia.Views;
using RecordTime.Avalonia.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using Serilog;

namespace RecordTime.Avalonia;

public partial class App : Application
{
    private TrayIcon? _trayIcon;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        // 配置 LiveCharts 主题
        LiveCharts.Configure(config =>
            config
                .AddSkiaSharp()
                .AddDefaultMappers()
                .AddLightTheme()
        );

        // 初始化多语言系统
        InitializeLanguageSystem();

        // 注册全局异常处理
        SetupExceptionHandling();
    }

    /// <summary>
    /// 初始化多语言系统
    /// </summary>
    private void InitializeLanguageSystem()
    {
        try
        {
            var settingsService = new RecordTime.Core.Services.AppSettingsService();
            var settings = settingsService.GetSettings();

            // 根据配置切换语言
            RecordTime.Avalonia.Resources.Strings.StringResources.Current.SwitchLanguage(settings.General.Language);

            Log.Information("语言系统已初始化: {Language}", settings.General.Language);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "初始化语言系统失败，使用默认中文");
            RecordTime.Avalonia.Resources.Strings.StringResources.Current.SwitchLanguage("zh-CN");
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            var mainViewModel = new MainWindowViewModel();
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            desktop.MainWindow = mainWindow;

            // 初始化系统托盘
            SetupTrayIcon(mainViewModel, mainWindow);

            // 跟踪是否正在退出
            var isShuttingDown = false;

            // 处理窗口关闭事件 - 最小化到托盘而不是退出
            mainWindow.Closing += (s, e) =>
            {
                if (!isShuttingDown)
                {
                    e.Cancel = true;
                    mainWindow.Hide();
                    Log.Debug("主窗口已最小化到托盘");
                }
            };

            // 在退出前设置标志
            desktop.ShutdownRequested += (s, e) =>
            {
                isShuttingDown = true;
            };

            // 处理应用退出 - 清理托盘图标
            desktop.Exit += (s, e) =>
            {
                _trayIcon?.Dispose();
                mainViewModel.Dispose();
                Log.Information("应用程序正在退出");
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// 设置系统托盘图标
    /// </summary>
    private void SetupTrayIcon(MainWindowViewModel mainViewModel, Window mainWindow)
    {
        try
        {
            // 初始化托盘服务
            var trayService = TrayIconService.Instance;
            trayService.Initialize(mainViewModel, mainWindow);

            // 加载托盘图标（默认为未监控状态）
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", "TrayIconIdle.ico");
            WindowIcon? defaultIcon = null;

            if (File.Exists(iconPath))
            {
                defaultIcon = new WindowIcon(iconPath);
                Log.Debug("托盘图标已加载: TrayIconIdle.ico");
            }
            else
            {
                Log.Warning("托盘图标文件不存在: {IconPath}, 使用主窗口图标作为后备", iconPath);
                defaultIcon = mainWindow.Icon;
            }

            // 创建托盘图标
            _trayIcon = new TrayIcon
            {
                Icon = defaultIcon,
                ToolTipText = "RecordTime - 时间追踪",
                IsVisible = true
            };

            // 点击托盘图标显示窗口
            _trayIcon.Clicked += (s, e) =>
            {
                trayService.ShowWindowCommand.Execute(null);
            };

            // 订阅监控状态变化以更新图标
            mainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsMonitoring))
                {
                    UpdateTrayIcon(mainViewModel.IsMonitoring);
                }
            };

            // 创建托盘菜单
            var menu = new NativeMenu();

            // 显示主窗口
            var showWindowItem = new NativeMenuItem("显示主窗口");
            showWindowItem.Click += (s, e) => trayService.ShowWindowCommand.Execute(null);
            menu.Add(showWindowItem);

            menu.Add(new NativeMenuItemSeparator());

            // 启动监控
            var startMonitoringItem = new NativeMenuItem("启动监控");
            startMonitoringItem.Click += async (s, e) => await trayService.StartMonitoringCommand.ExecuteAsync(null);
            menu.Add(startMonitoringItem);

            // 停止监控
            var stopMonitoringItem = new NativeMenuItem("停止监控");
            stopMonitoringItem.Click += async (s, e) => await trayService.StopMonitoringCommand.ExecuteAsync(null);
            menu.Add(stopMonitoringItem);

            menu.Add(new NativeMenuItemSeparator());

            // 开机自启动
            var autoStartItem = new NativeMenuItem("开机自启动")
            {
                ToggleType = NativeMenuItemToggleType.CheckBox,
                IsChecked = trayService.AutoStartEnabled
            };
            autoStartItem.Click += (s, e) =>
            {
                trayService.AutoStartEnabled = !trayService.AutoStartEnabled;
                autoStartItem.IsChecked = trayService.AutoStartEnabled;
            };
            menu.Add(autoStartItem);

            menu.Add(new NativeMenuItemSeparator());

            // 退出
            var exitItem = new NativeMenuItem("退出");
            exitItem.Click += (s, e) => trayService.ExitCommand.Execute(null);
            menu.Add(exitItem);

            _trayIcon.Menu = menu;

            Log.Information("系统托盘图标已创建");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建系统托盘图标失败");
        }
    }

    /// <summary>
    /// 更新托盘图标状态
    /// </summary>
    /// <param name="isMonitoring">是否正在监控</param>
    private void UpdateTrayIcon(bool isMonitoring)
    {
        try
        {
            if (_trayIcon == null) return;

            // 根据状态加载不同的图标
            var iconFileName = isMonitoring ? "TrayIconActive.ico" : "TrayIconIdle.ico";
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", iconFileName);

            if (File.Exists(iconPath))
            {
                _trayIcon.Icon = new WindowIcon(iconPath);
                Log.Debug("托盘图标已更新: {IconFile}", iconFileName);
            }
            else
            {
                Log.Warning("图标文件不存在: {IconPath}", iconPath);
            }

            // 更新提示文本以反映状态变化
            _trayIcon.ToolTipText = isMonitoring
                ? "RecordTime - 监控中 ⏱️"
                : "RecordTime - 未监控 ⏸️";

            Log.Debug("托盘图标状态已更新: {Status}", isMonitoring ? "监控中" : "未监控");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新托盘图标状态失败");
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    /// <summary>
    /// 设置全局异常处理
    /// </summary>
    private void SetupExceptionHandling()
    {
        // 使用统一的全局异常处理器
        GlobalExceptionHandler.Instance.RegisterGlobalHandlers();
    }
}