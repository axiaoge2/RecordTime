using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Core.Services;
using RecordTime.Core.Services.AICoach;
using RecordTime.Core.Models;
using RecordTime.Core.Exceptions;
using RecordTime.Data;
using RecordTime.Avalonia.Services;
using RecordTime.Avalonia.Resources.Strings;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly IWindowMonitor _windowMonitor;
    private readonly IInputMonitor _inputMonitor;
    private readonly IMediaDetector _mediaDetector;
    private readonly IActivityDetector _activityDetector;
    private readonly ConfigurationService _configurationService;
    private readonly BudgetTrackingService _budgetTrackingService;
    private readonly NotificationService _notificationService;
    private readonly AppDataService _appDataService;
    private readonly Func<SessionManager> _sessionManagerFactory;

    private SessionManager? _sessionManager;
    private System.Threading.Timer? _updateTimer;
    private int _timerExecuting = 0;
    private DateTime _lastCheckedDate = DateTime.Today;

    private ReportViewModel? _reportViewModel;
    private AppStatsViewModel? _appStatsViewModel;
    private SettingsViewModel? _settingsViewModel;
    private AboutViewModel? _aboutViewModel;
    private TimeBudgetViewModel? _timeBudgetViewModel;

    private readonly Func<AppStatsViewModel> _appStatsViewModelFactory;
    private readonly Func<ReportViewModel> _reportViewModelFactory;
    private readonly Func<TimeBudgetViewModel> _timeBudgetViewModelFactory;

    private AICoachViewModel? _aiCoachViewModel;
    private readonly Func<AICoachViewModel>? _aiCoachViewModelFactory;
    private readonly Func<SettingsViewModel> _settingsViewModelFactory;

    // 监控状态
    [ObservableProperty]
    private bool _isMonitoring = false;

    [ObservableProperty]
    private string _monitoringStatusText = StringResources.Current.MonitoringNotStarted;

    [ObservableProperty]
    private string _startButtonText = StringResources.Current.StartMonitoring;

    // 导航
    [ObservableProperty]
    private int _selectedTabIndex = 0;

    partial void OnSelectedTabIndexChanged(int value)
    {
        switch (value)
        {
            case 0: NavigateToDashboard(); break;
            case 1: NavigateToAppStats(); break;
            case 2: NavigateToReports(); break;
            case 3: NavigateToTimeBudget(); break;
            case 4: NavigateToSettings(); break;
            case 5: NavigateToAbout(); break;
        }
    }

    // 当前页面
    [ObservableProperty]
    private ViewModelBase? _currentPageViewModel;

    public bool ShowDashboard => CurrentPageViewModel is DashboardViewModel;

    partial void OnCurrentPageViewModelChanged(ViewModelBase? value)
    {
        OnPropertyChanged(nameof(ShowDashboard));
    }

    public DashboardViewModel DashboardVM { get; }

    public AICoachViewModel AICoachVM => _aiCoachViewModel ??= CreateAICoachViewModel();

    public MainWindowViewModel(
        IWindowMonitor windowMonitor,
        IInputMonitor inputMonitor,
        IMediaDetector mediaDetector,
        IActivityDetector activityDetector,
        ConfigurationService configurationService,
        BudgetTrackingService budgetTrackingService,
        NotificationService notificationService,
        AppDataService appDataService,
        DashboardViewModel dashboardViewModel,
        Func<SessionManager> sessionManagerFactory,
        Func<AppStatsViewModel> appStatsViewModelFactory,
        Func<ReportViewModel> reportViewModelFactory,
        Func<TimeBudgetViewModel> timeBudgetViewModelFactory,
        Func<SettingsViewModel> settingsViewModelFactory,
        Func<AICoachViewModel>? aiCoachViewModelFactory = null)
    {
        _windowMonitor = windowMonitor;
        _inputMonitor = inputMonitor;
        _mediaDetector = mediaDetector;
        _activityDetector = activityDetector;
        _configurationService = configurationService;
        _budgetTrackingService = budgetTrackingService;
        _notificationService = notificationService;
        _appDataService = appDataService;
        _sessionManagerFactory = sessionManagerFactory;
        _appStatsViewModelFactory = appStatsViewModelFactory;
        _reportViewModelFactory = reportViewModelFactory;
        _timeBudgetViewModelFactory = timeBudgetViewModelFactory;
        _settingsViewModelFactory = settingsViewModelFactory;
        _aiCoachViewModelFactory = aiCoachViewModelFactory;

        DashboardVM = dashboardViewModel;
        DashboardVM.SetToggleMonitoringAction(ToggleMonitoringAsync);

        _ = Task.Run(InitializeDatabaseAsync);

        CurrentPageViewModel = DashboardVM;
    }

    [RelayCommand]
    private async Task ToggleMonitoringAsync()
    {
        if (!IsMonitoring)
            await StartMonitoringAsync();
        else
            await StopMonitoringAsync();
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentPageViewModel = DashboardVM;
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
        _appStatsViewModel ??= _appStatsViewModelFactory();
        CurrentPageViewModel = _appStatsViewModel;
        ScheduleAfterNavigation(_appStatsViewModel.OnNavigatedToAsync);
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        _reportViewModel ??= _reportViewModelFactory();
        CurrentPageViewModel = _reportViewModel;
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        _settingsViewModel ??= _settingsViewModelFactory();
        CurrentPageViewModel = _settingsViewModel;
    }

    [RelayCommand]
    private void NavigateToAbout()
    {
        _aboutViewModel ??= new AboutViewModel();
        CurrentPageViewModel = _aboutViewModel;
    }

    [RelayCommand]
    private void NavigateToTimeBudget()
    {
        _timeBudgetViewModel ??= _timeBudgetViewModelFactory();
        CurrentPageViewModel = _timeBudgetViewModel;
        ScheduleAfterNavigation(_timeBudgetViewModel.OnNavigatedToAsync);
    }

    private AICoachViewModel CreateAICoachViewModel()
    {
        if (_aiCoachViewModelFactory != null)
        {
            try
            {
                var vm = _aiCoachViewModelFactory();
                _ = vm.InitializeAsync();
                Log.Information("AI Coach ViewModel 已创建");
                return vm;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "创建 AI Coach ViewModel 失败");
            }
        }
        return new AICoachViewModel();
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
            var config = _configurationService.Current;

            _sessionManager = _sessionManagerFactory();

            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.SessionEnded += OnSessionEnded;

            _sessionManager.Start();

            _budgetTrackingService.Start();
            _notificationService.Initialize(_budgetTrackingService);
            Log.Information("预算追踪和通知服务已启动");

            var refreshIntervalMs = config.Monitoring.DataRefreshIntervalMs;
            _updateTimer = new System.Threading.Timer(
                _ => Task.Run(async () =>
                {
                    if (Interlocked.CompareExchange(ref _timerExecuting, 1, 0) == 0)
                    {
                        try
                        {
                            var today = DateTime.Today;
                            if (today > _lastCheckedDate)
                            {
                                Log.Information("检测到跨零点：从 {OldDate} 切换到 {NewDate}",
                                    _lastCheckedDate.ToString("yyyy-MM-dd"), today.ToString("yyyy-MM-dd"));

                                if (DashboardVM.SelectedDate.Date == _lastCheckedDate)
                                {
                                    await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                                    {
                                        DashboardVM.SelectedDate = today;
                                        DashboardVM.SelectedDateText = today.ToString(StringResources.Current.DateFormatPattern);
                                    });
                                }

                                _lastCheckedDate = today;
                            }

                            if (IsMonitoring && DashboardVM.SelectedDate.Date == DateTime.Today)
                            {
                                await DashboardVM.LoadDataForDateAsync(DashboardVM.SelectedDate);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "定时刷新数据时发生错误");
                        }
                        finally
                        {
                            Interlocked.Exchange(ref _timerExecuting, 0);
                        }
                    }
                }),
                null,
                TimeSpan.FromMilliseconds(refreshIntervalMs),
                TimeSpan.FromMilliseconds(refreshIntervalMs)
            );

            IsMonitoring = true;
            DashboardVM.IsMonitoring = true;
            MonitoringStatusText = StringResources.Current.MonitoringRunning;
            DashboardVM.MonitoringStatusText = MonitoringStatusText;
            DashboardVM.DataStatusHint = StringResources.Current.RealTimeData;
            StartButtonText = StringResources.Current.StopMonitoring;
            DashboardVM.StartButtonText = StartButtonText;

            Log.Information("专注已成功启动");

            await DashboardVM.LoadDataForDateAsync(DashboardVM.SelectedDate);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "启动专注失败");
            MonitoringStatusText = StringResources.Current.StartMonitoringFailed;
            GlobalExceptionHandler.Instance.HandleException(
                MonitoringException.StartupFailed("SessionManager", ex));
        }
    }

    private async Task StopMonitoringAsync()
    {
        try
        {
            _updateTimer?.Dispose();
            _updateTimer = null;

            _budgetTrackingService.Stop();
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
            DashboardVM.IsMonitoring = false;
            MonitoringStatusText = StringResources.Current.MonitoringNotStarted;
            DashboardVM.MonitoringStatusText = MonitoringStatusText;
            DashboardVM.DataStatusHint = StringResources.Current.ShowHistoricalData;
            StartButtonText = StringResources.Current.StartMonitoring;
            DashboardVM.StartButtonText = StartButtonText;

            await DashboardVM.LoadDataForDateAsync(DashboardVM.SelectedDate);
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
        _budgetTrackingService.TriggerImmediateUpdate();
        ScheduleDebouncedRefresh();
    }

    private void ScheduleDebouncedRefresh()
    {
        if (DashboardVM.SelectedDate.Date != DateTime.Today) return;

        _sessionEventDebounceTimer?.Dispose();
        _sessionEventDebounceTimer = new System.Threading.Timer(
            _ => _ = DashboardVM.LoadDataForDateAsync(DashboardVM.SelectedDate),
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

            var canConnect = await dbContext.Database.CanConnectAsync();

            if (!canConnect)
            {
                Log.Information("数据库不存在，正在创建...");
                await dbContext.Database.MigrateAsync();
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");
                Log.Information("数据库创建成功");
            }
            else
            {
                await dbContext.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;");

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
                        Log.Warning("数据库表已存在但缺少 Migrations 历史记录，尝试同步状态...");

                        var allMigrations = dbContext.Database.GetMigrations();
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
            GlobalExceptionHandler.Instance.HandleException(DatabaseException.MigrationFailed(sqliteEx));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "数据库 Migrations 应用失败");
            GlobalExceptionHandler.Instance.HandleException(DatabaseException.MigrationFailed(ex));
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

    private async Task AutoFixIncompleteSessionsAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var incompleteSessions = await dbContext.Sessions
                .Where(s => s.EndTime == null)
                .ToListAsync();

            if (incompleteSessions.Count == 0)
            {
                Log.Debug("启动检查: 没有发现未结束的会话");
                return;
            }

            Log.Information("启动检查: 发现 {Count} 个未结束的会话,正在自动修复...", incompleteSessions.Count);

            foreach (var session in incompleteSessions)
            {
                session.EndTime = session.StartTime.AddMinutes(5);
                session.DurationSeconds = 300;
            }

            await dbContext.SaveChangesAsync();
            Log.Information("会话自动修复完成: 已修复 {Count} 个会话", incompleteSessions.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "自动修复会话时发生错误");
        }
    }

    private async Task FixStaleSessionsAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var now = DateTime.Now;
            var staleThreshold = now.AddMinutes(-2);

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
                if (session.LastHeartbeat != null)
                {
                    session.EndTime = session.LastHeartbeat;
                    session.DurationSeconds = (int)(session.LastHeartbeat.Value - session.StartTime).TotalSeconds;
                }
                else
                {
                    session.EndTime = session.StartTime.AddMinutes(5);
                    session.DurationSeconds = 300;
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

    public void Dispose()
    {
        _updateTimer?.Dispose();
        _sessionEventDebounceTimer?.Dispose();

        DashboardVM.Dispose();
        (_reportViewModel as IDisposable)?.Dispose();
        (_appStatsViewModel as IDisposable)?.Dispose();
        (_timeBudgetViewModel as IDisposable)?.Dispose();
        (_aiCoachViewModel as IDisposable)?.Dispose();

        _budgetTrackingService.Stop();
        _notificationService.Dispose();

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
    }
}
