using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Media.Imaging;
using System;
using System.IO;
using System.Linq;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using RecordTime.Avalonia.ViewModels;
using RecordTime.Avalonia.Views;
using RecordTime.Avalonia.Services;
using RecordTime.Core.Services;
using RecordTime.Core.Services.AICoach;
using RecordTime.Data;
using RecordTime.Data.Repositories;
using Serilog;

namespace RecordTime.Avalonia;

public partial class App : Application
{
    private TrayIcon? _trayIcon;

    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        ThemeService.Instance.Initialize();
        InitializeLanguageSystem();
        SetupExceptionHandling();
    }

    private void InitializeLanguageSystem()
    {
        try
        {
            var settingsService = new RecordTime.Core.Services.AppSettingsService();
            var settings = settingsService.GetSettings();
            RecordTime.Avalonia.Resources.Strings.StringResources.Current.SwitchLanguage(settings.General.Language);
            Log.Information("语言系统已初始化: {Language}", settings.General.Language);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "初始化语言系统失败，使用默认中文");
            RecordTime.Avalonia.Resources.Strings.StringResources.Current.SwitchLanguage("zh-CN");
        }
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // --- Core 层：监控服务（Singleton，管理全局钩子）---
        services.AddSingleton<ConfigurationService>();
        services.AddSingleton<IConfigurationService>(sp => sp.GetRequiredService<ConfigurationService>());

        services.AddSingleton<IWindowMonitor>(sp =>
        {
            var config = sp.GetRequiredService<ConfigurationService>().Current;
            return new WindowMonitor(config.Monitoring.WindowPollIntervalMs);
        });
        services.AddSingleton<IInputMonitor, InputMonitor>();
        services.AddSingleton<IMediaDetector, MediaDetector>();
        services.AddSingleton<IActivityDetector, ActivityDetector>();
        services.AddSingleton<IIconExtractor, IconExtractor>();

        // --- Data 层 ---
        services.AddTransient<RecordTimeDbContext>();
        services.AddTransient<ISessionRepository>(sp =>
        {
            var dbContext = sp.GetRequiredService<RecordTimeDbContext>();
            return new SessionRepository(dbContext, ownsContext: true);
        });

        // --- Avalonia 层：应用服务 ---
        services.AddSingleton<AppSettingsService>();
        services.AddSingleton<TrayIconService>(sp => TrayIconService.Instance);
        services.AddSingleton<AppDataService>();
        services.AddSingleton<BudgetTrackingService>();
        services.AddSingleton<NotificationService>();

        // SessionManager 工厂（每次启动监控时创建新实例）
        services.AddSingleton<Func<SessionManager>>(sp => () =>
        {
            var config = sp.GetRequiredService<ConfigurationService>().Current;
            return new SessionManager(
                sp.GetRequiredService<IWindowMonitor>(),
                sp.GetRequiredService<IInputMonitor>(),
                sp.GetRequiredService<IMediaDetector>(),
                sp.GetRequiredService<IActivityDetector>(),
                () =>
                {
                    var dbContext = new RecordTimeDbContext();
                    return new SessionRepository(dbContext, ownsContext: true);
                },
                config.Monitoring.IdleTimeoutSeconds
            );
        });

        // --- ViewModels ---
        services.AddSingleton<DashboardViewModel>();

        services.AddTransient<AppStatsViewModel>();
        services.AddSingleton<Func<AppStatsViewModel>>(sp => () => sp.GetRequiredService<AppStatsViewModel>());

        services.AddTransient<ReportViewModel>();
        services.AddSingleton<Func<ReportViewModel>>(sp => () => sp.GetRequiredService<ReportViewModel>());

        services.AddTransient<TimeBudgetViewModel>();
        services.AddSingleton<Func<TimeBudgetViewModel>>(sp => () => sp.GetRequiredService<TimeBudgetViewModel>());

        services.AddTransient<SettingsViewModel>();
        services.AddSingleton<Func<SettingsViewModel>>(sp => () => sp.GetRequiredService<SettingsViewModel>());

        // AI Coach ViewModel 工厂
        services.AddSingleton<Func<AICoachViewModel>>(sp => () =>
        {
            var knowledgeBase = new KnowledgeBaseProvider();
            var promptBuilder = new PromptBuilder(knowledgeBase);
            var windowMonitor = sp.GetRequiredService<IWindowMonitor>();
            var contextBuilder = new CognitiveContextBuilder(
                () =>
                {
                    var dbContext = new RecordTimeDbContext();
                    return new SessionRepository(dbContext, ownsContext: true);
                },
                windowMonitor
            );
            return new AICoachViewModel(contextBuilder, promptBuilder);
        });

        services.AddSingleton<MainWindowViewModel>();

        return services.BuildServiceProvider();
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            Services = ConfigureServices();

            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();
            var mainWindow = new MainWindow
            {
                DataContext = mainViewModel,
            };

            desktop.MainWindow = mainWindow;

            SetupTrayIcon(mainViewModel, mainWindow);

            var settingsService = Services.GetRequiredService<AppSettingsService>();
            var isShuttingDown = false;

            mainWindow.Closing += (s, e) =>
            {
                if (!isShuttingDown && settingsService.GetSettings().General.MinimizeToTray)
                {
                    e.Cancel = true;
                    mainWindow.Hide();
                    Log.Debug("主窗口已最小化到托盘");
                }
            };

            desktop.ShutdownRequested += (s, e) =>
            {
                isShuttingDown = true;
            };

            desktop.Exit += (s, e) =>
            {
                _trayIcon?.Dispose();
                mainViewModel.Dispose();
                Log.Information("应用程序正在退出");
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void SetupTrayIcon(MainWindowViewModel mainViewModel, Window mainWindow)
    {
        try
        {
            var trayService = TrayIconService.Instance;
            trayService.Initialize(mainViewModel, mainWindow);

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

            _trayIcon = new TrayIcon
            {
                Icon = defaultIcon,
                ToolTipText = "RecordTime - 时间追踪",
                IsVisible = true
            };

            _trayIcon.Clicked += (s, e) =>
            {
                trayService.ShowWindowCommand.Execute(null);
            };

            mainViewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.IsMonitoring))
                {
                    UpdateTrayIcon(mainViewModel.IsMonitoring);
                }
            };

            var menu = new NativeMenu();

            var showWindowItem = new NativeMenuItem("显示主窗口");
            showWindowItem.Click += (s, e) => trayService.ShowWindowCommand.Execute(null);
            menu.Add(showWindowItem);

            menu.Add(new NativeMenuItemSeparator());

            var startMonitoringItem = new NativeMenuItem("开始专注");
            startMonitoringItem.Click += async (s, e) => await trayService.StartMonitoringCommand.ExecuteAsync(null);
            menu.Add(startMonitoringItem);

            var stopMonitoringItem = new NativeMenuItem("停止专注");
            stopMonitoringItem.Click += async (s, e) => await trayService.StopMonitoringCommand.ExecuteAsync(null);
            menu.Add(stopMonitoringItem);

            menu.Add(new NativeMenuItemSeparator());

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

    private void UpdateTrayIcon(bool isMonitoring)
    {
        try
        {
            if (_trayIcon == null) return;

            var iconFileName = isMonitoring ? "TrayIconActive.ico" : "TrayIconIdle.ico";
            var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", iconFileName);

            if (File.Exists(iconPath))
            {
                _trayIcon.Icon = new WindowIcon(iconPath);
            }

            _trayIcon.ToolTipText = isMonitoring
                ? "RecordTime - 专注中 ⏱️"
                : "RecordTime - 未开始 ⏸️";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "更新托盘图标状态失败");
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }

    private void SetupExceptionHandling()
    {
        GlobalExceptionHandler.Instance.RegisterGlobalHandlers();
    }
}
