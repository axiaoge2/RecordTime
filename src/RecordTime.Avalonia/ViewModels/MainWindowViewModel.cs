using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Core.Services;
using RecordTime.Core.Models;
using RecordTime.Data;
using RecordTime.Data.Repositories;
using RecordTime.Avalonia.Services;
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
    private readonly IIconExtractor _iconExtractor;
    private int _timerExecuting = 0; // 0 = 未执行, 1 = 执行中 (防重入)

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
    private string _monitoringStatusText = "监控未启动";

    [ObservableProperty]
    private string _startButtonText = "启动监控";

    // 日期选择
    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _selectedDateText = DateTime.Today.ToString("yyyy年MM月dd日");

    // 分类统计
    public ObservableCollection<CategoryStatItem> CategoryStats { get; } = new();

    // TOP 应用
    public ObservableCollection<TopAppItem> TopApps { get; } = new();

    // 饼图数据 - TOP 5 应用使用时长
    [ObservableProperty]
    private ISeries[] _pieChartSeries = Array.Empty<ISeries>();

    // 条形图数据 - 分类统计
    [ObservableProperty]
    private ISeries[] _barChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _barChartXAxes = Array.Empty<Axis>();

    // 应用类型饼图数据 - 用于大型圆环图
    [ObservableProperty]
    private ISeries[] _appTypePieChartSeries = Array.Empty<ISeries>();

    // 圆环图中心显示的类型名称（使用最多的类型）
    [ObservableProperty]
    private string _topCategoryName = "暂无数据";

    // 圆环图中心显示的类型时长
    [ObservableProperty]
    private string _topCategoryDuration = "--";

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
        // 创建监控服务
        _windowMonitor = new WindowMonitor();
        _inputMonitor = new InputMonitor();
        _mediaDetector = new MediaDetector();
        _activityDetector = new ActivityDetector();
        _iconExtractor = new IconExtractor();

        // 应用 EF Core Migrations (自动创建/更新数据库 schema)
        _ = ApplyDatabaseMigrationsAsync();

        // 执行数据库迁移（更新旧数据的 DisplayName）
        _ = MigrateDisplayNamesAsync();

        // 加载今日数据（只加载一次，不自动刷新）
        _ = LoadDataForDateAsync(SelectedDate);

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
        SelectedDateText = SelectedDate.ToString("yyyy年MM月dd日");
        await LoadDataForDateAsync(SelectedDate);
    }

    [RelayCommand]
    private async Task NextDayAsync()
    {
        SelectedDate = SelectedDate.AddDays(1);
        SelectedDateText = SelectedDate.ToString("yyyy年MM月dd日");
        await LoadDataForDateAsync(SelectedDate);
    }

    [RelayCommand]
    private async Task TodayAsync()
    {
        SelectedDate = DateTime.Today;
        SelectedDateText = SelectedDate.ToString("yyyy年MM月dd日");
        await LoadDataForDateAsync(SelectedDate);
    }

    [RelayCommand]
    private void NavigateToDashboard()
    {
        CurrentPageViewModel = null; // null 表示显示主仪表盘
    }

    [RelayCommand]
    private void NavigateToAppStats()
    {
        CurrentPageViewModel = new AppStatsViewModel();
    }

    [RelayCommand]
    private void NavigateToReports()
    {
        CurrentPageViewModel = new ReportViewModel();
    }

    [RelayCommand]
    private void NavigateToSettings()
    {
        CurrentPageViewModel = new SettingsViewModel();
    }

    [RelayCommand]
    private void NavigateToAbout()
    {
        CurrentPageViewModel = new AboutViewModel();
    }

    private async Task StartMonitoringAsync()
    {
        try
        {
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
                }
            );

            // 订阅事件
            _sessionManager.SessionStarted += OnSessionStarted;
            _sessionManager.SessionEnded += OnSessionEnded;

            // 启动监控
            _sessionManager.Start();

            // 启动定时刷新（每 1 秒，只在监控运行且查看今日数据时自动刷新）
            _updateTimer = new System.Threading.Timer(
                async _ =>
                {
                    // 防重入: 如果上一次执行还未完成,跳过本次
                    if (System.Threading.Interlocked.CompareExchange(ref _timerExecuting, 1, 0) == 0)
                    {
                        try
                        {
                            if (IsMonitoring && SelectedDate.Date == DateTime.Today)
                            {
                                await LoadDataForDateAsync(SelectedDate);
                            }
                        }
                        finally
                        {
                            System.Threading.Interlocked.Exchange(ref _timerExecuting, 0);
                        }
                    }
                },
                null,
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(1)
            );

            IsMonitoring = true;
            MonitoringStatusText = "监控运行中 - 正在实时追踪您的活动";
            StartButtonText = "停止监控";

            // 立即刷新数据
            await LoadDataForDateAsync(SelectedDate);
        }
        catch (Exception ex)
        {
            MonitoringStatusText = $"启动失败: {ex.Message}";
        }
    }

    private async Task StopMonitoringAsync()
    {
        try
        {
            // 停止并销毁定时刷新
            _updateTimer?.Dispose();
            _updateTimer = null;

            if (_sessionManager != null)
            {
                await _sessionManager.StopAsync();
                _sessionManager.SessionStarted -= OnSessionStarted;
                _sessionManager.SessionEnded -= OnSessionEnded;
                _sessionManager.Dispose();
                _sessionManager = null;
            }

            IsMonitoring = false;
            MonitoringStatusText = "监控已停止";
            StartButtonText = "启动监控";

            // 最后刷新一次数据
            await LoadDataForDateAsync(SelectedDate);
        }
        catch (Exception ex)
        {
            MonitoringStatusText = $"停止失败: {ex.Message}";
        }
    }

    private void OnSessionStarted(object? sender, AppSession session)
    {
        System.Diagnostics.Debug.WriteLine($"🟢 会话开始: {session.DisplayName} (ProcessName: {session.ProcessName})");

        // 会话开始时刷新数据（只在查看今日数据时）
        if (SelectedDate.Date == DateTime.Today)
        {
            System.Diagnostics.Debug.WriteLine("触发数据刷新...");
            _ = LoadDataForDateAsync(SelectedDate);
        }
    }

    private void OnSessionEnded(object? sender, AppSession session)
    {
        System.Diagnostics.Debug.WriteLine($"🔴 会话结束: {session.DisplayName} (ID: {session.Id}, Duration: {session.DurationSeconds}s)");

        // 会话结束时刷新数据（只在查看今日数据时）
        if (SelectedDate.Date == DateTime.Today)
        {
            System.Diagnostics.Debug.WriteLine("触发数据刷新...");
            _ = LoadDataForDateAsync(SelectedDate);
        }
    }

    private async Task ApplyDatabaseMigrationsAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            // 自动应用所有待执行的 Migrations
            await dbContext.Database.MigrateAsync();

            System.Diagnostics.Debug.WriteLine("✅ 数据库 Migrations 应用成功");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ 数据库 Migrations 应用失败: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"DisplayName迁移失败: {ex.Message}");
        }
    }

    private async Task LoadDataForDateAsync(DateTime date)
    {
        // 使用锁防止并发更新导致重复数据
        await _loadDataLock.WaitAsync();
        try
        {
            // 使用共享的 AppDataService 获取数据快照
            var appDataService = AppDataService.Instance;
            var snapshot = await appDataService.GetSnapshotAsync(date);

            System.Diagnostics.Debug.WriteLine($"=== MainWindow: 获取快照 [{snapshot.GetDebugInfo()}] ===");

            // 计算数据指纹 (session count + total seconds)
            var currentFingerprint = $"{snapshot.SessionCount}_{snapshot.TotalSeconds}";

            // 如果数据没有变化,跳过UI更新以避免图表重绘
            if (currentFingerprint == _lastDataFingerprint)
            {
                System.Diagnostics.Debug.WriteLine("=== 数据未变化,跳过UI更新 ===");
                return;
            }

            _lastDataFingerprint = currentFingerprint;
            System.Diagnostics.Debug.WriteLine($"=== 检测到数据变化: {_lastDataFingerprint} ===");

            if (snapshot.AllApps.Count == 0)
            {
                // 没有数据
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
                TopCategoryName = "暂无数据";
                TopCategoryDuration = "--";
                _lastDataFingerprint = string.Empty; // 重置指纹
                return;
            }

            // 从快照计算汇总数据
            var hours = snapshot.TotalSeconds / 3600;
            var minutes = (snapshot.TotalSeconds % 3600) / 60;
            TotalDuration = $"{hours:D2}h {minutes:D2}m";

            SessionCount = snapshot.SessionCount;
            AppTypeCount = snapshot.AllApps.Select(a => a.Category).Distinct().Count();

            // ActivityType 需要从数据库查询（因为 AppDataItem 没有这个字段）
            await using var dbContext = new RecordTimeDbContext();
            var targetDate = date.Date;
            ActivityTypeCount = await dbContext.Sessions
                .AsNoTracking() // 只读查询,不需要跟踪实体变化
                .Where(s => s.StartTime >= targetDate && s.StartTime < targetDate.AddDays(1))
                .Select(s => s.ActivityType)
                .Distinct()
                .CountAsync();

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

            // 在 UI 线程上更新所有集合
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                // 更新分类统计
                CategoryStats.Clear();
                foreach (var item in categoryGroups)
                {
                    CategoryStats.Add(item);
                }

                // 更新 TOP 10 应用 - 直接使用 AllApps 取前10个(与应用统计页面保持一致)
                TopApps.Clear();
                var top10Apps = snapshot.AllApps.Take(10).ToList();

                System.Diagnostics.Debug.WriteLine($"=== MainWindow: 开始更新 TOP 10 ===");
                System.Diagnostics.Debug.WriteLine($"  AllApps.Count = {snapshot.AllApps.Count}");
                System.Diagnostics.Debug.WriteLine($"  top10Apps.Count = {top10Apps.Count}");

                for (int i = 0; i < top10Apps.Count; i++)
                {
                    var app = top10Apps[i];

                    // 提取应用图标
                    var icon = _iconExtractor.ExtractIcon(app.ProcessName);

                    var item = new TopAppItem
                    {
                        Rank = i + 1,
                        AppName = app.AppName,
                        Duration = app.TotalDuration,
                        SessionCount = app.SessionCount,
                        Icon = icon
                    };
                    TopApps.Add(item);
                    System.Diagnostics.Debug.WriteLine($"  添加 #{item.Rank}: {item.AppName} - {item.Duration.TotalSeconds:F0}s (图标: {(icon != null ? "✓" : "✗")})");
                }
                System.Diagnostics.Debug.WriteLine($"=== MainWindow: UI 更新完成，TopApps.Count = {TopApps.Count} ===");

                // 更新饼图数据 - TOP 5 应用
                var top5Apps = snapshot.AllApps.Take(5).ToList();
                var colors = new[]
                {
                    new SKColor(102, 126, 234),   // #667eea - 主色调
                    new SKColor(118, 75, 162),    // #764ba2 - 次色调
                    new SKColor(237, 100, 166),   // 粉色
                    new SKColor(255, 154, 0),     // 橙色
                    new SKColor(52, 199, 89)      // 绿色
                };

                PieChartSeries = top5Apps.Select((app, index) => new PieSeries<double>
                {
                    Values = new[] { app.TotalDuration.TotalMinutes },
                    Name = app.AppName,
                    Fill = new SolidColorPaint(colors[index % colors.Length])
                    {
                        Color = colors[index % colors.Length]
                    },
                    DataLabelsPaint = new SolidColorPaint(new SKColor(29, 29, 31)),
                    DataLabelsSize = 12,
                    DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                    DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F0}m"
                }).Cast<ISeries>().ToArray();

                // 更新条形图数据 - 分类统计
                if (categoryGroups.Count > 0)
                {
                    var categoryNames = categoryGroups.Select(c => c.Category).ToArray();
                    var categoryValues = categoryGroups.Select(c => c.Duration.TotalMinutes).ToArray();

                    BarChartSeries = new ISeries[]
                    {
                        new ColumnSeries<double>
                        {
                            Values = categoryValues,
                            Fill = new LinearGradientPaint(
                                new[] { new SKColor(102, 126, 234), new SKColor(118, 75, 162) },
                                new SKPoint(0, 0),
                                new SKPoint(0, 1)
                            ),
                            DataLabelsPaint = new SolidColorPaint(new SKColor(29, 29, 31)),
                            DataLabelsSize = 11,
                            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                            DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F0}m"
                        }
                    };

                    BarChartXAxes = new Axis[]
                    {
                        new Axis
                        {
                            Labels = categoryNames,
                            LabelsRotation = 15,
                            LabelsPaint = new SolidColorPaint(new SKColor(110, 110, 115)),
                            SeparatorsPaint = new SolidColorPaint(new SKColor(229, 229, 234))
                        }
                    };
                }
                else
                {
                    BarChartSeries = Array.Empty<ISeries>();
                    BarChartXAxes = Array.Empty<Axis>();
                }

                // 更新应用类型圆环图数据 - TOP 5 应用分类
                var appTypeGroups = snapshot.AllApps
                    .GroupBy(a => a.Category)
                    .Select(g => new
                    {
                        Category = g.Key,
                        TotalMinutes = g.Sum(a => a.TotalDuration.TotalMinutes)
                    })
                    .OrderByDescending(x => x.TotalMinutes)
                    .Take(5)
                    .ToList();

                if (appTypeGroups.Count > 0)
                {
                    // 使用更大更鲜艳的颜色配置（适合大型圆环图）
                    var donutColors = new[]
                    {
                        new SKColor(102, 126, 234),   // #667eea - 主色调
                        new SKColor(237, 100, 166),   // #ed64a6 - 粉色
                        new SKColor(255, 154, 0),     // #ff9a00 - 橙色
                        new SKColor(52, 199, 89),     // #34c759 - 绿色
                        new SKColor(118, 75, 162)     // #764ba2 - 紫色
                    };

                    AppTypePieChartSeries = appTypeGroups.Select((group, index) => new PieSeries<double>
                    {
                        Values = new[] { group.TotalMinutes },
                        Name = group.Category,
                        Fill = new SolidColorPaint(donutColors[index % donutColors.Length])
                        {
                            Color = donutColors[index % donutColors.Length]
                        },
                        InnerRadius = 80,  // 设置内半径创建圆环效果
                        HoverPushout = 8  // 悬停时突出距离
                    }).Cast<ISeries>().ToArray();

                    // 设置圆环图中心显示（最多使用的类型）
                    var topCategory = appTypeGroups.First();
                    TopCategoryName = topCategory.Category;
                    var hours = (int)(topCategory.TotalMinutes / 60);
                    var minutes = (int)(topCategory.TotalMinutes % 60);
                    TopCategoryDuration = hours > 0 ? $"{hours}h {minutes}m" : $"{minutes}m";
                }
                else
                {
                    AppTypePieChartSeries = Array.Empty<ISeries>();
                    TopCategoryName = "暂无数据";
                    TopCategoryDuration = "--";
                }
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载数据失败: {ex.Message}");
        }
        finally
        {
            _loadDataLock.Release();
        }
    }

    public void Dispose()
    {
        _updateTimer?.Dispose();

        // ✅ 修复: 先异步停止 SessionManager,确保当前会话被正确结束
        // 这样可以避免应用退出时会话 EndTime 为 null 导致时长异常的问题
        if (_sessionManager != null)
        {
            try
            {
                _sessionManager.StopAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"⚠️ Dispose 时停止 SessionManager 失败: {ex.Message}");
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

    public string RankText => $"#{Rank}";
    public string DurationText => $"{(int)Duration.TotalHours:D2}:{Duration.Minutes:D2}:{Duration.Seconds:D2}";
    public string SessionCountText => $"{SessionCount} 次";
}
