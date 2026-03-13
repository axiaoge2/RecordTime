using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Core.Services;
using RecordTime.Core.Services.AICoach;
using RecordTime.Core.Models;
using RecordTime.Core.Exceptions;
using RecordTime.Data;
using RecordTime.Data.Repositories;
using RecordTime.Avalonia.Services;
using RecordTime.Avalonia.Resources.Strings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Serilog;
using Avalonia.Media;
using System.Collections.Generic;

namespace RecordTime.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IWindowMonitor _windowMonitor;
    private readonly IInputMonitor _inputMonitor;
    private readonly IMediaDetector _mediaDetector;
    private readonly IActivityDetector _activityDetector;
    private SessionManager? _sessionManager;
    private System.Threading.Timer? _updateTimer;
    private readonly SemaphoreSlim _loadDataLock = new(1, 1);
    private CancellationTokenSource? _dashboardLoadCancellationTokenSource;
    private CancellationTokenSource? _dashboardIconLoadCancellationTokenSource;
    private readonly IIconExtractor _iconExtractor;
    private int _timerExecuting = 0; // 0 = 未执行, 1 = 执行中 (防重入)
    private DateTime _lastCheckedDate = DateTime.Today; // 跨零点检测：记录上次检测的日期

    // 页面 ViewModel 单例 - 保持页面状态,避免切换时丢失进度
    private ReportViewModel? _reportViewModel;
    private AppStatsViewModel? _appStatsViewModel;
    private SettingsViewModel? _settingsViewModel;
    private AboutViewModel? _aboutViewModel;
    private TimeBudgetViewModel? _timeBudgetViewModel;

    // AI Coach ViewModel
    private AICoachViewModel? _aiCoachViewModel;

    // 汇总数据
    [ObservableProperty]
    private string _totalDuration = "00h 00m";

    [ObservableProperty]
    private int _sessionCount = 0;

    [ObservableProperty]
    private int _appTypeCount = 0;

    [ObservableProperty]
    private int _activityTypeCount = 0;

    // 上一次的数据指纹,用于检测数据是否真正变化
    private string _lastDataFingerprint = string.Empty;

    // 监控状态
    [ObservableProperty]
    private bool _isMonitoring = false;

    [ObservableProperty]
    private string _monitoringStatusText = StringResources.Current.MonitoringNotStarted;

    [ObservableProperty]
    private string _startButtonText = StringResources.Current.StartMonitoring;

    // 数据状态提示
    [ObservableProperty]
    private string _dataStatusHint = StringResources.Current.ShowHistoricalData;

    [ObservableProperty]
    private string _dataUpdateTime = "--";

    [ObservableProperty]
    private bool _showEmptyState = false;

    // 日期选择
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _selectedDateText = DateTime.Today.ToString(StringResources.Current.DateFormatPattern);

    // 分类统计
    public BulkObservableCollection<CategoryStatItem> CategoryStats { get; } = new();

    // TOP 应用
    public BulkObservableCollection<TopAppItem> TopApps { get; } = new();

    // 饼图数据 - TOP 5 应用使用时长
    [ObservableProperty]
    private ISeries[] _pieChartSeries = Array.Empty<ISeries>();

    // 条形图数据 - 分类统计
    [ObservableProperty]
    private ISeries[] _barChartSeries = Array.Empty<ISeries>();

    // 导航索引控制
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    partial void OnSelectedTabIndexChanged(int value)
    {
        switch (value)
        {
            case 0: // Dashboard
                NavigateToDashboard();
                break;
            case 1: // AppStats
                NavigateToAppStats();
                break;
            case 2: // Reports
                NavigateToReports();
                break;
            case 3: // TimeBudget
                NavigateToTimeBudget();
                break;
            case 4: // Settings
                NavigateToSettings();
                break;
            case 5: // About
                NavigateToAbout();
                break;
        }
    }

    [ObservableProperty]
    private Axis[] _barChartXAxes = Array.Empty<Axis>();

    // 应用类型饼图数据 - 用于大型圆环图
    [ObservableProperty]
    private ISeries[] _appTypePieChartSeries = Array.Empty<ISeries>();

    // 圆环图中心显示的类型名称（使用最多的类型）
    [ObservableProperty]
    private string _topCategoryName = StringResources.Current.NoData;

    // 圆环图中心显示的类型时长
    [ObservableProperty]
    private string _topCategoryDuration = "--";

    // ========== Phase 4: 预算进度面板 ==========

    /// <summary>
    /// 预算进度列表
    /// </summary>
    public BulkObservableCollection<BudgetProgressDisplayItem> BudgetProgressItems { get; } = new();

    /// <summary>
    /// 总预算数量
    /// </summary>
    [ObservableProperty]
    private int _totalBudgetCount = 0;

    /// <summary>
    /// 已完成/达标的预算数量
    /// </summary>
    [ObservableProperty]
    private int _completedBudgetCount = 0;

    /// <summary>
    /// 超标的预算数量
    /// </summary>
    [ObservableProperty]
    private int _overBudgetCount = 0;

    /// <summary>
    /// 是否有预算
    /// </summary>
    public bool HasBudgets => TotalBudgetCount > 0;

    /// <summary>
    /// 是否有超标预算
    /// </summary>
    public bool HasOverBudget => OverBudgetCount > 0;

    partial void OnTotalBudgetCountChanged(int value) => OnPropertyChanged(nameof(HasBudgets));
    partial void OnOverBudgetCountChanged(int value) => OnPropertyChanged(nameof(HasOverBudget));

    // ========== AI Coach ==========

    /// <summary>
    /// AI Coach ViewModel - 用于悬浮面板绑定
    /// </summary>
    public AICoachViewModel AICoachVM => _aiCoachViewModel ??= CreateAICoachViewModel();

    // 当前页面内容
    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    // 是否显示仪表盘
    public bool ShowDashboard => CurrentPageViewModel == null;

    partial void OnCurrentPageViewModelChanged(ViewModelBase? value)
    {
        OnPropertyChanged(nameof(ShowDashboard));
    }

    public MainWindowViewModel()
    {
        // 加载配置
        var config = ConfigurationService.Instance.Current;

        // 创建监控服务
        _windowMonitor = new WindowMonitor(config.Monitoring.WindowPollIntervalMs);
        _inputMonitor = new InputMonitor();
        _mediaDetector = new MediaDetector();
        _activityDetector = new ActivityDetector();
        _iconExtractor = new IconExtractor();

        // 应用 EF Core Migrations (自动创建/更新数据库 schema)
        _ = Task.Run(InitializeDatabaseAsync);

        // 执行数据库迁移（更新旧数据的 DisplayName）

        // 自动修复未结束的会话（Phase 1 数据完整性保障）

        // Phase 1 Task 1.2: 检测并修复心跳过期的会话

        // Phase 4: 订阅预算进度更新事件
        BudgetTrackingService.Instance.ProgressUpdated += OnBudgetProgressUpdated;

        // 加载今日数据（只加载一次，不自动刷新）
        _ = LoadDataForDateAsync(SelectedDate);

        // Phase 4: 加载预算进度
        _ = LoadBudgetProgressAsync();

        // 初始化页面导航
        _appStatsViewModel ??= new AppStatsViewModel();
        // 默认显示 Dashboard
        CurrentPageViewModel = this;

        // 每次导航到此页面时刷新数据（异步执行，不阻塞UI）

        // 注意：定时刷新将在启动监控时创建，停止监控时销毁
    }

    [RelayCommand]
    private async Task ToggleMonitoringAsync()
    {
        if (!IsMonitoring)
        {
            await StartMonitoringAsync();
        }
        else
        {
            await StopMonitoringAsync();
        }
    }

    [RelayCommand]
    private async Task PreviousDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(-1);
        SelectedDateText = SelectedDate.ToString(StringResources.Current.DateFormatPattern);
        await LoadDataForDateAsync(SelectedDate);
    }

    [RelayCommand]
    private async Task NextDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(1);
        SelectedDateText = SelectedDate.ToString(StringResources.Current.DateFormatPattern);
        await LoadDataForDateAsync(SelectedDate);
    }

    [RelayCommand]
    private async Task TodayAsync()
    {
        SelectedDate = DateTime.Today;
        SelectedDateText = SelectedDate.ToString(StringResources.Current.DateFormatPattern);
        await LoadDataForDateAsync(SelectedDate);
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentPageViewModel = this;
    }

    private void ScheduleAfterNavigation(Func<Task> action)
    {
        _ = global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(async () =>
        {
            await Task.Yield();
            await action();
        }, global::Avalonia.Threading.DispatcherPriority.Background);
    }

    [RelayCommand]
    private void NavigateToAppStats()
    {
        // 使用单例模式 - 避免重复创建 ViewModel,保持页面状态
        _appStatsViewModel ??= new AppStatsViewModel();
        CurrentPageViewModel = _appStatsViewModel;

        // 每次导航到此页面时刷新数据（异步执行，不阻塞UI）
        ScheduleAfterNavigation(_appStatsViewModel.OnNavigatedToAsync);
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        // 使用单例模式 - 避免重复创建 ViewModel,保持报告生成进度
        _reportViewModel ??= new ReportViewModel();
        CurrentPageViewModel = _reportViewModel;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        // 使用单例模式 - 避免重复创建 ViewModel,保持设置页面状态
        _settingsViewModel ??= new SettingsViewModel();
        CurrentPageViewModel = _settingsViewModel;
    }

    [RelayCommand]
    private void NavigateToAbout()
    {
        // 使用单例模式 - 避免重复创建 ViewModel
        _aboutViewModel ??= new AboutViewModel();
        CurrentPageViewModel = _aboutViewModel;
    }

    [RelayCommand]
    private void NavigateToTimeBudget()
    {
        // 使用单例模式 - 保持时间目标页面状态
        _timeBudgetViewModel ??= new TimeBudgetViewModel();
        CurrentPageViewModel = _timeBudgetViewModel;

        // 每次导航到此页面时刷新进度数据（异步执行，不阻塞UI）
        ScheduleAfterNavigation(_timeBudgetViewModel.OnNavigatedToAsync);
    }

    /// <summary>
    /// 创建 AI Coach ViewModel
    /// </summary>
    private AICoachViewModel CreateAICoachViewModel()
    {
        try
        {
            // 创建服务依赖
            var knowledgeBase = new KnowledgeBaseProvider();
            var promptBuilder = new PromptBuilder(knowledgeBase);

            // 创建上下文构建器
            var contextBuilder = new CognitiveContextBuilder(
                () =>
                {
                    var dbContext = new RecordTimeDbContext();
                    return new SessionRepository(dbContext, ownsContext: true);
                },
                _windowMonitor
            );

            // 使用新的构造函数，让 ViewModel 自己管理配置
            var vm = new AICoachViewModel(contextBuilder, promptBuilder);

            // 异步初始化 (检查服务可用性)
            _ = vm.InitializeAsync();

            Log.Information("AI Coach ViewModel 已创建");
            return vm;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建 AI Coach ViewModel 失败");
            return new AICoachViewModel(); // 返回空的 ViewModel
        }
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            // 加载配置
            var config = ConfigurationService.Instance.Current;

            // 创建 SessionManager
            _sessionManager = new SessionManager(
                _windowMonitor,
                _inputMonitor,
                _mediaDetector,
                _activityDetector,
                () =>
                {
                    var dbContext = new RecordTimeDbContext();
                    return new SessionRepository(dbContext, ownsContext: true);
                },
                config.Monitoring.IdleTimeoutSeconds
            );

            // 订阅事件
            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.SessionEnded += OnSessionEnded;

            // 启动监控
            _sessionManager.Start();

            // 启动预算追踪服务和通知服务
            var budgetTrackingService = BudgetTrackingService.Instance;
            budgetTrackingService.Start();
            NotificationService.Instance.Initialize(budgetTrackingService);
            Log.Information("预算追踪和通知服务已启动");

            // 启动定时刷新（只在监控运行且查看今日数据时自动刷新）
            // 注意：使用 Task.Run 包装避免 async void 导致异常无法捕获
            var refreshIntervalMs = config.Monitoring.DataRefreshIntervalMs;
            _updateTimer = new System.Threading.Timer(
                _ => Task.Run(async () =>
                {
                    // 防重入: 如果上一次执行还未完成,跳过本次
                    if (System.Threading.Interlocked.CompareExchange(ref _timerExecuting, 1, 0) == 0)
                    {
                        try
                        {
                            // 跨零点检测：如果日期发生变化，自动切换到新的"今日"
                            var today = DateTime.Today;
                            if (today > _lastCheckedDate)
                            {
                                Log.Information("检测到跨零点：从 {OldDate} 切换到 {NewDate}",
                                    _lastCheckedDate.ToString("yyyy-MM-dd"),
                                    today.ToString("yyyy-MM-dd"));

                                // 如果用户之前查看的是"昨日"（现在看来），自动切换到新的"今日"
                                if (SelectedDate.Date == _lastCheckedDate)
                                {
                                    // 在 UI 线程更新日期显示
                                    await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        SelectedDate = today;
                                        SelectedDateText = today.ToString(StringResources.Current.DateFormatPattern);
                                    });
                                    Log.Information("已自动切换仪表盘日期到新的今日");
                                }

                                _lastCheckedDate = today;
                            }

                            if (IsMonitoring && SelectedDate.Date == DateTime.Today)
                            {
                                await LoadDataForDateAsync(SelectedDate);
                            }
                        }
                        catch (Exception ex)
                        {
                            // 定时器中的异常不应中断监控，只记录日志
                            Log.Error(ex, "定时刷新数据时发生错误");
                        }
                        finally
                        {
                            System.Threading.Interlocked.Exchange(ref _timerExecuting, 0);
                        }
                    }
                }),
                null,
                TimeSpan.FromMilliseconds(refreshIntervalMs),
                TimeSpan.FromMilliseconds(refreshIntervalMs)
            );

            IsMonitoring = true;
            MonitoringStatusText = StringResources.Current.MonitoringRunning;
            DataStatusHint = StringResources.Current.RealTimeData;
            StartButtonText = StringResources.Current.StopMonitoring;

            Log.Information("监控已成功启动");

            // 立即刷新数据
            await LoadDataForDateAsync(SelectedDate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动监控失败");

            // 显示用户友好的错误消息
            MonitoringStatusText = StringResources.Current.StartMonitoringFailed;

            // 通过全局异常处理器显示错误对话框
            GlobalExceptionHandler.Instance.HandleException(
                MonitoringException.StartupFailed("SessionManager", ex));
        }
    }

    private async Task StopMonitoringAsync()
    {
        try
        {
            // 停止并销毁定时刷新
            _updateTimer?.Dispose();
            _updateTimer = null;

            // 停止预算追踪服务
            BudgetTrackingService.Instance.Stop();
            Log.Information("预算追踪服务已停止");

            if (_sessionManager != null)
            {
                await _sessionManager.StopAsync();
                _sessionManager.SessionStarted -= OnSessionStarted;
                _sessionManager.SessionEnded -= OnSessionEnded;
                _sessionManager.Dispose();
                _sessionManager = null;
            }

            IsMonitoring = false;
            MonitoringStatusText = StringResources.Current.MonitoringNotStarted;
            DataStatusHint = StringResources.Current.ShowHistoricalData;
            StartButtonText = StringResources.Current.StartMonitoring;

            // 最后刷新一次数据
            await LoadDataForDateAsync(SelectedDate);
        }
        catch (Exception ex)
        {
            MonitoringStatusText = StringResources.Current.StopFailedPrefix + ex.Message;
        }
    }

    private System.Threading.Timer? _sessionEventDebounceTimer;

    private void OnSessionStarted(object? sender, AppSession session)
    {
        Log.Debug("会话开始: {DisplayName} (ProcessName: {ProcessName})", session.DisplayName, session.ProcessName);
        ScheduleDebouncedRefresh();
    }

    private void OnSessionEnded(object? sender, AppSession session)
    {
        Log.Debug("会话结束: {DisplayName} (ID: {SessionId}, Duration: {DurationSeconds}s)", session.DisplayName, session.Id, session.DurationSeconds);
        BudgetTrackingService.Instance.TriggerImmediateUpdate();
        ScheduleDebouncedRefresh();
    }

    private void ScheduleDebouncedRefresh()
    {
        if (SelectedDate.Date != DateTime.Today) return;

        _sessionEventDebounceTimer?.Dispose();
        _sessionEventDebounceTimer = new System.Threading.Timer(
            _ => _ = LoadDataForDateAsync(SelectedDate),
            null,
            dueTime: 500,
            period: Timeout.Infinite);
    }

    private async Task InitializeDatabaseAsync()
    {
        await ApplyDatabaseMigrationsAsync().ConfigureAwait(false);
        await MigrateDisplayNamesAsync().ConfigureAwait(false);
        await AutoFixIncompleteSessionsAsync().ConfigureAwait(false);
        await FixStaleSessionsAsync().ConfigureAwait(false);
    }

    private async Task ApplyDatabaseMigrationsAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            // 检查数据库是否存在
            var canConnect = await dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                // 数据库不存在，创建并应用 Migrations
                Log.Information("数据库不存在，正在创建...");
                await dbContext.Database.MigrateAsync();
                
                // 启用 WAL 模式以提高性能
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                
                Log.Information("数据库创建成功");
            }
            else
            {
                // 确保现有数据库也启用 WAL 模式
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

                // 数据库已存在，检查是否有待应用的 Migrations
                var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();

                if (pendingMigrations.Any())
                {
                    Log.Information("检测到 {Count} 个待应用的 Migrations，正在应用...", pendingMigrations.Count());

                    try
                    {
                        await dbContext.Database.MigrateAsync();
                        Log.Information("Migrations 应用成功");
                    }
                    catch (Microsoft.Data.Sqlite.SqliteException sqliteEx) when (sqliteEx.SqliteErrorCode == 1)
                    {
                        // SQLite Error 1: table already exists
                        // 这种情况发生在数据库已存在但没有 Migrations 历史记录时
                        Log.Warning("数据库表已存在但缺少 Migrations 历史记录，尝试同步状态...");

                        // 获取所有 Migrations
                        var allMigrations = dbContext.Database.GetMigrations();

                        // 手动插入 Migration 历史记录（标记为已应用）
                        foreach (var migration in allMigrations)
                        {
                            var sql = $"INSERT OR IGNORE INTO __EFMigrationsHistory (MigrationId, ProductVersion) VALUES ('{migration}', '7.0.0')";
                            await dbContext.Database.ExecuteSqlRawAsync(sql);
                        }

                        Log.Information("Migrations 历史记录已同步");
                    }
                }
                else
                {
                    Log.Debug("数据库 schema 已是最新版本，无需应用 Migrations");
                }
            }
        }
        catch (Microsoft.Data.Sqlite.SqliteException sqliteEx)
        {
            Log.Error(sqliteEx, "数据库 Migrations 应用失败 (SQLite Error {ErrorCode})", sqliteEx.SqliteErrorCode);

            // 通过全局异常处理器显示错误
            GlobalExceptionHandler.Instance.HandleException(
                DatabaseException.MigrationFailed(sqliteEx));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库 Migrations 应用失败");

            // 通过全局异常处理器显示错误
            GlobalExceptionHandler.Instance.HandleException(
                DatabaseException.MigrationFailed(ex));
        }
    }

    private async Task MigrateDisplayNamesAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();
            await RecordTime.Data.Migrations.DisplayNameMigration.MigrateDisplayNamesAsync(dbContext);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DisplayName 迁移失败");
        }
    }

    /// <summary>
    /// Phase 1 任务 1.1: 应用启动时自动修复未结束的会话
    /// </summary>
    /// <remarks>
    /// 此方法在应用启动时静默执行,无需用户干预:
    /// - 检测 EndTime 为 null 的异常会话
    /// - 将 EndTime 设置为 StartTime + 5分钟
    /// - 将 DurationSeconds 设置为 300秒
    /// - 记录所有修复操作到日志以供审计
    ///
    /// 设计原则:
    /// 1. 静默执行,不打断用户体验
    /// 2. 详细日志记录,便于问题追溯
    /// 3. 异常容错,即使失败也不影响应用启动
    /// </remarks>
    private async Task AutoFixIncompleteSessionsAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            // 查找所有未结束的会话
            var incompleteSessions = await dbContext.Sessions
                .Where(s => s.EndTime == null)
                .ToListAsync();

            if (incompleteSessions.Count == 0)
            {
                Log.Debug("启动检查: 没有发现未结束的会话");
                return;
            }

            Log.Information("启动检查: 发现 {Count} 个未结束的会话,正在自动修复...", incompleteSessions.Count);

            // 修复每个会话
            foreach (var session in incompleteSessions)
            {
                // 修复策略: EndTime = StartTime + 5分钟
                session.EndTime = session.StartTime.AddMinutes(5);
                session.DurationSeconds = 300;

                Log.Debug("修复会话 {Id}: {ProcessName} (DisplayName: {DisplayName}, StartTime: {StartTime})",
                    session.Id,
                    session.ProcessName,
                    session.DisplayName,
                    session.StartTime);
            }

            // 保存修改
            await dbContext.SaveChangesAsync();

            Log.Information("会话自动修复完成: 已修复 {Count} 个会话", incompleteSessions.Count);
        }
        catch (Exception ex)
        {
            // 异常不应影响应用启动,只记录日志
            Log.Error(ex, "自动修复会话时发生错误");
        }
    }

    /// <summary>
    /// Phase 1 任务 1.2: 检测并修复心跳过期的会话
    /// </summary>
    /// <remarks>
    /// 此方法在应用启动时静默执行:
    /// - 检测 EndTime 为 null 且 LastHeartbeat 超过2分钟的会话
    /// - 将 EndTime 设置为 LastHeartbeat (如果存在) 或 StartTime + 5分钟 (如果不存在)
    /// - 计算实际持续时长
    /// - 记录所有修复操作
    ///
    /// 设计原则:
    /// 1. 使用心跳时间作为会话结束时间,更准确反映实际使用情况
    /// 2. 兼容旧数据(无心跳记录)
    /// 3. 2分钟阈值:心跳间隔30秒,允许4个心跳周期的容错
    /// </remarks>
    private async Task FixStaleSessionsAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var now = DateTime.Now;
            var staleThreshold = now.AddMinutes(-2); // 心跳超过2分钟视为过期

            // 查找所有未结束但心跳过期的会话
            var staleSessions = await dbContext.Sessions
                .Where(s => s.EndTime == null &&
                           (s.LastHeartbeat == null || s.LastHeartbeat < staleThreshold))
                .ToListAsync();

            if (staleSessions.Count == 0)
            {
                Log.Debug("启动检查: 没有发现心跳过期的会话");
                return;
            }

            Log.Information("启动检查: 发现 {Count} 个心跳过期的会话,正在自动修复...", staleSessions.Count);

            foreach (var session in staleSessions)
            {
                // 修复策略:
                // - 如果有心跳记录,使用 LastHeartbeat 作为 EndTime
                // - 如果没有心跳记录,使用 StartTime + 5分钟 作为 EndTime (兼容旧数据)
                if (session.LastHeartbeat != null)
                {
                    session.EndTime = session.LastHeartbeat;
                    session.DurationSeconds = (int)(session.LastHeartbeat.Value - session.StartTime).TotalSeconds;
                    Log.Debug("修复会话 {Id} (基于心跳): {ProcessName}, StartTime={StartTime}, LastHeartbeat={LastHeartbeat}",
                        session.Id,
                        session.ProcessName,
                        session.StartTime,
                        session.LastHeartbeat);
                }
                else
                {
                    session.EndTime = session.StartTime.AddMinutes(5);
                    session.DurationSeconds = 300;
                    Log.Debug("修复会话 {Id} (无心跳,使用默认5分钟): {ProcessName}, StartTime={StartTime}",
                        session.Id,
                        session.ProcessName,
                        session.StartTime);
                }
            }

            await dbContext.SaveChangesAsync();

            Log.Information("心跳过期会话修复完成: 已修复 {Count} 个会话", staleSessions.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "修复心跳过期会话时发生错误");
        }
    }

    private async Task LoadDataForDateAsync(DateTime date)
    {
        _dashboardLoadCancellationTokenSource?.Cancel();
        _dashboardLoadCancellationTokenSource?.Dispose();
        _dashboardLoadCancellationTokenSource = new CancellationTokenSource();
        var loadToken = _dashboardLoadCancellationTokenSource.Token;

        _dashboardIconLoadCancellationTokenSource?.Cancel();
        _dashboardIconLoadCancellationTokenSource?.Dispose();
        _dashboardIconLoadCancellationTokenSource = new CancellationTokenSource();
        var iconToken = _dashboardIconLoadCancellationTokenSource.Token;

        // 使用锁防止并发更新导致重复数据
        await _loadDataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            // 使用共享的 AppDataService 获取数据快照
            var appDataService = AppDataService.Instance;
            var snapshot = await appDataService.GetSnapshotAsync(date).ConfigureAwait(false);

            if (loadToken.IsCancellationRequested)
            {
                return;
            }

            Log.Debug("MainWindow: 获取快照 [{DebugInfo}]", snapshot.GetDebugInfo());

            // 计算数据指纹 (session count + total seconds)
            var currentFingerprint = $"{snapshot.SessionCount}_{snapshot.TotalSeconds}";

            // 如果数据没有变化,跳过UI更新以避免图表重绘
            if (currentFingerprint == _lastDataFingerprint)
            {
                Log.Debug("数据未变化,跳过UI更新");
                return;
            }

            _lastDataFingerprint = currentFingerprint;
            Log.Debug("检测到数据变化: {DataFingerprint}", _lastDataFingerprint);

            // 更新数据时间戳
            // - 监控运行中: 显示实时查询时间
            // - 监控未运行: 显示最后一条会话的结束时间
            var dataUpdateTimeText = "--";
            if (IsMonitoring && date.Date == DateTime.Today)
            {
                dataUpdateTimeText = DateTime.Now.ToString("HH:mm");
            }
            else
            {
                // 查询最后一条会话的结束时间
                await using var db = new RecordTimeDbContext();
                var dateStart = date.Date;
                var lastSession = await db.Sessions
                    .AsNoTracking()
                    .Where(s => s.StartTime >= dateStart && s.StartTime < dateStart.AddDays(1) && s.EndTime != null)
                    .OrderByDescending(s => s.EndTime)
                    .Select(s => s.EndTime)
                    .FirstOrDefaultAsync().ConfigureAwait(false);

                dataUpdateTimeText = lastSession.HasValue ? lastSession.Value.ToString("HH:mm") : "--";
            }

            if (snapshot.AllApps.Count == 0)
            {
                // 没有数据 - 显示空状态
                await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    DataUpdateTime = dataUpdateTimeText;
                    ShowEmptyState = true;
                    TotalDuration = "00h 00m";
                    SessionCount = 0;
                    AppTypeCount = 0;
                    ActivityTypeCount = 0;
                    CategoryStats.Clear();
                    TopApps.Clear();
                    PieChartSeries = Array.Empty<ISeries>();
                    BarChartSeries = Array.Empty<ISeries>();
                    BarChartXAxes = Array.Empty<Axis>();
                    AppTypePieChartSeries = Array.Empty<ISeries>();
                    TopCategoryName = StringResources.Current.NoData;
                    TopCategoryDuration = "--";
                }, global::Avalonia.Threading.DispatcherPriority.Background);
                _lastDataFingerprint = string.Empty; // 重置指纹
                return;
            }

            // 有数据 - 隐藏空状态

            // 从快照计算汇总数据
            var hours = snapshot.TotalSeconds / 3600;
            var minutes = (snapshot.TotalSeconds % 3600) / 60;
            var totalDurationText = $"{hours:D2}h {minutes:D2}m";

            var sessionCount = snapshot.SessionCount;
            var appTypeCount = snapshot.AllApps.Select(a => a.Category).Distinct().Count();

            // ActivityType 需要从数据库查询（因为 AppDataItem 没有这个字段）
            await using var dbContext = new RecordTimeDbContext();
            var targetDate = date.Date;
            var activityTypeCount = await dbContext.Sessions
                .AsNoTracking() // 只读查询,不需要跟踪实体变化
                .Where(s => s.StartTime >= targetDate && s.StartTime < targetDate.AddDays(1))
                .Select(s => s.ActivityType)
                .Distinct()
                .CountAsync().ConfigureAwait(false);

            // 从快照计算分类统计
            var categoryGroups = snapshot.AllApps
                .GroupBy(a => a.Category)
                .Select(g => new CategoryStatItem
                {
                    Category = g.Key,
                    Duration = TimeSpan.FromSeconds(g.Sum(a => a.TotalDuration.TotalSeconds)),
                    Percentage = g.Sum(a => a.TotalPercentage)
                })
                .OrderByDescending(x => x.Duration)
                .Take(5)
                .ToList();

            // 在后台线程预计算所有数据和图表 Series
            var top10Apps = snapshot.AllApps.Take(10).ToList();
            var top5Apps = snapshot.AllApps.Take(5).ToList();
            var iconRequests = new List<(TopAppItem Item, string ProcessName, string Category)>();

            var topAppItems = new List<TopAppItem>();
            for (int i = 0; i < top10Apps.Count; i++)
            {
                var app = top10Apps[i];
                var item = new TopAppItem
                {
                    Rank = i + 1,
                    AppName = app.AppName,
                    Duration = app.TotalDuration,
                    SessionCount = app.SessionCount,
                    Percentage = app.TotalPercentage
                };
                topAppItems.Add(item);
                iconRequests.Add((item, app.ProcessName, app.Category));
            }

            var pieColors = new[]
            {
                new SKColor(102, 126, 234), new SKColor(118, 75, 162),
                new SKColor(237, 100, 166), new SKColor(255, 154, 0), new SKColor(52, 199, 89)
            };
            var pieChartData = top5Apps.Select((app, index) => new PieSeries<double>
            {
                Values = new[] { app.TotalDuration.TotalMinutes },
                Name = app.AppName,
                Fill = new SolidColorPaint(pieColors[index % pieColors.Length]),
                DataLabelsPaint = new SolidColorPaint(new SKColor(29, 29, 31)),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F0}m"
            }).Cast<ISeries>().ToArray();

            var categoryColorMap = new Dictionary<string, (SKColor Start, SKColor End)>
            {
                { "开发工具", (new SKColor(0x3F, 0xA6, 0xFF), new SKColor(0x1E, 0x74, 0xD8)) },
                { "办公软件", (new SKColor(0x35, 0xD0, 0xC8), new SKColor(0x1A, 0xA4, 0xA2)) },
                { "视频娱乐", (new SKColor(0xFF, 0x7E, 0xB3), new SKColor(0xE8, 0x5A, 0x9C)) },
                { "社交通讯", (new SKColor(0x6A, 0xD1, 0xFF), new SKColor(0x2A, 0x9B, 0xEA)) },
                { "游戏", (new SKColor(0x7F, 0xD0, 0x6A), new SKColor(0x45, 0xA8, 0x4F)) },
                { "浏览器", (new SKColor(0x9B, 0x7B, 0xFF), new SKColor(0x6B, 0x53, 0xE5)) },
                { "系统工具", (new SKColor(0x55, 0xB5, 0xA0), new SKColor(0x2E, 0x8B, 0x79)) },
                { "其他", (new SKColor(0xA1, 0xB7, 0xFF), new SKColor(0x6C, 0x7F, 0xD9)) },
            };
            var defaultColor = (Start: new SKColor(0xA1, 0xB7, 0xFF), End: new SKColor(0x6C, 0x7F, 0xD9));

            ISeries[] barChartData;
            Axis[] barXAxes;
            if (categoryGroups.Count > 0)
            {
                var seriesList = new List<ISeries>();
                var categoryNames = new List<string>();
                foreach (var category in categoryGroups)
                {
                    var cc = categoryColorMap.GetValueOrDefault(category.Category, defaultColor);
                    categoryNames.Add(category.Category);
                    seriesList.Add(new ColumnSeries<double>
                    {
                        Values = new[] { category.Duration.TotalMinutes },
                        Name = category.Category,
                        Fill = new LinearGradientPaint(new[] { cc.Start, cc.End }, new SKPoint(0, 0), new SKPoint(0, 1)),
                        DataLabelsPaint = new SolidColorPaint(new SKColor(29, 29, 31)),
                        DataLabelsSize = 11,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F0}m"
                    });
                }
                barChartData = seriesList.ToArray();
                barXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = categoryNames.ToArray(), LabelsRotation = 15,
                        LabelsPaint = new SolidColorPaint(new SKColor(110, 110, 115)),
                        SeparatorsPaint = new SolidColorPaint(new SKColor(229, 229, 234))
                    }
                };
            }
            else
            {
                barChartData = Array.Empty<ISeries>();
                barXAxes = Array.Empty<Axis>();
            }

            var appTypeGroups = snapshot.AllApps
                .GroupBy(a => a.Category)
                .Select(g => new { Category = g.Key, TotalMinutes = g.Sum(a => a.TotalDuration.TotalMinutes) })
                .OrderByDescending(x => x.TotalMinutes).Take(5).ToList();

            ISeries[] donutData;
            string topCatName;
            string topCatDuration;
            if (appTypeGroups.Count > 0)
            {
                var donutColors = new[]
                {
                    new SKColor(102, 126, 234), new SKColor(237, 100, 166),
                    new SKColor(255, 154, 0), new SKColor(52, 199, 89), new SKColor(118, 75, 162)
                };
                donutData = appTypeGroups.Select((group, index) => new PieSeries<double>
                {
                    Values = new[] { group.TotalMinutes }, Name = group.Category,
                    Fill = new SolidColorPaint(donutColors[index % donutColors.Length]),
                    InnerRadius = 80, HoverPushout = 8
                }).Cast<ISeries>().ToArray();

                var topCat = appTypeGroups.First();
                topCatName = topCat.Category;
                var h = (int)(topCat.TotalMinutes / 60);
                var m = (int)(topCat.TotalMinutes % 60);
                topCatDuration = h > 0 ? $"{h}h {m}m" : $"{m}m";
            }
            else
            {
                donutData = Array.Empty<ISeries>();
                topCatName = StringResources.Current.NoData;
                topCatDuration = "--";
            }

            if (loadToken.IsCancellationRequested) return;

            // UI 线程只做赋值
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                DataUpdateTime = dataUpdateTimeText;
                ShowEmptyState = false;
                TotalDuration = totalDurationText;
                SessionCount = sessionCount;
                AppTypeCount = appTypeCount;
                ActivityTypeCount = activityTypeCount;

                CategoryStats.ReplaceWith(categoryGroups);
                TopApps.ReplaceWith(topAppItems);

                PieChartSeries = pieChartData;
                BarChartSeries = barChartData;
                BarChartXAxes = barXAxes;
                AppTypePieChartSeries = donutData;
                TopCategoryName = topCatName;
                TopCategoryDuration = topCatDuration;
            }, global::Avalonia.Threading.DispatcherPriority.Background);

            _ = Task.Run(async () =>
            {
                try { await LoadTopAppIconsAsync(iconRequests, iconToken).ConfigureAwait(false); }
                catch (OperationCanceledException) { }
            }, iconToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载数据失败");
        }
        finally
        {
            _loadDataLock.Release();
        }
    }

    /// <summary>
    /// Phase 4: 加载预算进度数据
    /// </summary>
    private async Task LoadTopAppIconsAsync(
        List<(TopAppItem Item, string ProcessName, string Category)> requests,
        CancellationToken cancellationToken)
    {
        foreach (var (item, processName, category) in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var icon = _iconExtractor.ExtractIcon(processName, category);
            if (icon == null)
            {
                continue;
            }

            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
                () => item.Icon = icon,
                global::Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private async Task LoadBudgetProgressAsync()
    {
        try
        {
            var progressItems = await BudgetTrackingService.Instance.GetCurrentProgressAsync().ConfigureAwait(false);
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateBudgetProgressUI(progressItems));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载预算进度失败");
        }
    }

    /// <summary>
    /// Phase 4: 处理预算进度更新事件
    /// </summary>
    private void OnBudgetProgressUpdated(object? sender, List<BudgetProgressItem> progressItems)
    {
        Log.Debug("MainWindow: 收到预算进度更新, {Count} 个预算", progressItems.Count);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateBudgetProgressUI(progressItems));
    }

    /// <summary>
    /// Phase 4: 更新预算进度 UI
    /// </summary>
    private void UpdateBudgetProgressUI(List<BudgetProgressItem> progressItems)
    {
        int completed = 0;
        int overBudget = 0;

        var displayItems = new List<BudgetProgressDisplayItem>(progressItems.Count);
        foreach (var item in progressItems)
        {
            var displayItem = new BudgetProgressDisplayItem
            {
                DisplayName = item.Budget.DisplayName,
                TypeLabel = item.Budget.Type == BudgetType.Maximum
                    ? StringResources.Current.UpperLimitLabel
                    : StringResources.Current.LowerLimitLabel,
                BudgetType = item.Budget.Type,
                ProgressPercentage = item.ProgressPercentage,
                ProgressText = FormatProgressText(item.TodayActualMinutes, item.Budget.TargetMinutes),
                StatusText = FormatStatusText(item),
                IsOverBudget = item.IsOverBudget,
                IsGoalMet = item.IsGoalMet
            };
            displayItem.UpdateProgressColor();
            displayItems.Add(displayItem);

            if (item.IsGoalMet) completed++;
            if (item.IsOverBudget) overBudget++;
        }

        BudgetProgressItems.ReplaceWith(displayItems);
        TotalBudgetCount = progressItems.Count;
        CompletedBudgetCount = completed;
        OverBudgetCount = overBudget;

        Log.Debug("预算进度UI更新完成: {Total}个预算, {Completed}个达标, {Over}个超标",
            TotalBudgetCount, CompletedBudgetCount, OverBudgetCount);
    }

    /// <summary>
    /// 格式化进度文本 (例如: "45分钟 / 2小时")
    /// </summary>
    private static string FormatProgressText(int actualMinutes, int targetMinutes)
    {
        var actualText = FormatDuration(actualMinutes);
        var targetText = FormatDuration(targetMinutes);
        return $"{actualText} / {targetText}";
    }

    /// <summary>
    /// 格式化状态文本 (例如: "剩余1小时15分钟" 或 "已超出30分钟")
    /// </summary>
    private static string FormatStatusText(BudgetProgressItem item)
    {
        if (item.Budget.Type == BudgetType.Maximum)
        {
            // 上限目标
            if (item.IsOverBudget)
            {
                var overMinutes = item.TodayActualMinutes - item.Budget.TargetMinutes;
                return string.Format(StringResources.Current.OverTimeFormat, FormatDuration(overMinutes));
            }
            else
            {
                return string.Format(StringResources.Current.RemainingTimeFormat, FormatDuration(item.RemainingMinutes));
            }
        }
        else
        {
            // 下限目标
            if (item.IsGoalMet)
            {
                return StringResources.Current.GoalMetLabel;
            }
            else
            {
                return string.Format(StringResources.Current.RemainingTimeFormat, FormatDuration(item.RemainingMinutes));
            }
        }
    }

    /// <summary>
    /// 格式化时长 (分钟转为可读格式)
    /// </summary>
    private static string FormatDuration(int minutes)
    {
        if (minutes >= 60)
        {
            var hours = minutes / 60;
            var mins = minutes % 60;
            return mins > 0 ? $"{hours}h{mins}m" : $"{hours}h";
        }
        return $"{minutes}m";
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();
        _sessionEventDebounceTimer?.Dispose();
        _dashboardLoadCancellationTokenSource?.Cancel();
        _dashboardLoadCancellationTokenSource?.Dispose();
        _dashboardLoadCancellationTokenSource = null;

        _dashboardIconLoadCancellationTokenSource?.Cancel();
        _dashboardIconLoadCancellationTokenSource?.Dispose();
        _dashboardIconLoadCancellationTokenSource = null;

        BudgetTrackingService.Instance.ProgressUpdated -= OnBudgetProgressUpdated;
        BudgetTrackingService.Instance.Stop();
        NotificationService.Instance.Dispose();

        if (_sessionManager != null)
        {
            try
            {
                Task.Run(() => _sessionManager.StopAsync()).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Dispose 时停止 SessionManager 失败");
            }
            finally
            {
                _sessionManager.Dispose();
                _sessionManager = null;
            }
        }

        (_inputMonitor as IDisposable)?.Dispose();
        _loadDataLock.Dispose();
    }
}

// 分类统计项
public partial class CategoryStatItem : ObservableObject
{
    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private double _percentage;

    public string DurationText => $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
    public string PercentageText => $"{Percentage:F1}%";
}

// TOP 应用项
public partial class TopAppItem : ObservableObject
{
    [ObservableProperty]
    private int _rank;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private TimeSpan _duration;

    [ObservableProperty]
    private int _sessionCount;

    [ObservableProperty]
    private global::Avalonia.Media.Imaging.Bitmap? _icon;

    [ObservableProperty]
    private double _percentage;

    public string RankText => $"#{Rank}";
    public string DurationText => $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
    public string SessionCountText => $"{SessionCount}{StringResources.Current.UsageCountSuffix}";
    public string PercentageText => $"{Percentage:F1}%";
}

/// <summary>
/// Phase 4: 预算进度显示项 - 用于仪表盘显示
/// </summary>
public partial class BudgetProgressDisplayItem : ObservableObject
{
    /// <summary>
    /// 显示名称
    /// </summary>
    [ObservableProperty]
    private string _displayName = string.Empty;

    /// <summary>
    /// 类型标签 (上限/下限)
    /// </summary>
    [ObservableProperty]
    private string _typeLabel = string.Empty;

    /// <summary>
    /// 预算类型
    /// </summary>
    [ObservableProperty]
    private BudgetType _budgetType;

    /// <summary>
    /// 完成百分比 (0-100)
    /// </summary>
    [ObservableProperty]
    private double _progressPercentage;

    /// <summary>
    /// 进度文本 (例如: "45分钟 / 2小时")
    /// </summary>
    [ObservableProperty]
    private string _progressText = string.Empty;

    /// <summary>
    /// 状态文本 (例如: "剩余1小时15分钟")
    /// </summary>
    [ObservableProperty]
    private string _statusText = string.Empty;

    /// <summary>
    /// 是否超出预算
    /// </summary>
    [ObservableProperty]
    private bool _isOverBudget;

    /// <summary>
    /// 是否已达标
    /// </summary>
    [ObservableProperty]
    private bool _isGoalMet;

    /// <summary>
    /// 进度条颜色
    /// </summary>
    [ObservableProperty]
    private IBrush _progressColor = new SolidColorBrush(Color.Parse("#4ECDC4"));

    /// <summary>
    /// 根据状态更新进度条颜色
    /// </summary>
    public void UpdateProgressColor()
    {
        string colorHex;
        if (BudgetType == BudgetType.Maximum)
        {
            // 上限目标：绿色表示在范围内，红色表示超出
            if (IsOverBudget) colorHex = "#FF6B6B"; // 红色
            else if (ProgressPercentage >= 80) colorHex = "#FFB347"; // 橙色警告
            else colorHex = "#4ECDC4"; // 绿色
        }
        else
        {
            // 下限目标：绿色表示达标，橙色表示未达标
            if (IsGoalMet) colorHex = "#4ECDC4"; // 绿色
            else if (ProgressPercentage >= 50) colorHex = "#FFB347"; // 橙色
            else colorHex = "#8E8E93"; // 灰色
        }
        ProgressColor = new SolidColorBrush(Color.Parse(colorHex));
    }
}
