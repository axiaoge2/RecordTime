using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Core.Models;
using RecordTime.Core.Services;
using RecordTime.Data;
using RecordTime.Avalonia.Services;
using RecordTime.Avalonia.Resources.Strings;
using System;
using System.Collections.Generic;
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

namespace RecordTime.Avalonia.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly AppDataService _appDataService;
    private readonly BudgetTrackingService _budgetTrackingService;
    private readonly IIconExtractor _iconExtractor;
    private readonly SemaphoreSlim _loadDataLock = new(1, 1);
    private CancellationTokenSource? _dashboardLoadCancellationTokenSource;
    private CancellationTokenSource? _dashboardIconLoadCancellationTokenSource;
    private string _lastDataFingerprint = string.Empty;

    [ObservableProperty]
    private string _totalDuration = "00h 00m";

    [ObservableProperty]
    private int _sessionCount = 0;

    [ObservableProperty]
    private int _appTypeCount = 0;

    [ObservableProperty]
    private int _activityTypeCount = 0;

    [ObservableProperty]
    private string _dataStatusHint = StringResources.Current.ShowHistoricalData;

    [ObservableProperty]
    private string _dataUpdateTime = "--";

    [ObservableProperty]
    private bool _showEmptyState = false;

    [ObservableProperty]
    private DateTime _selectedDate = DateTime.Today;

    [ObservableProperty]
    private string _selectedDateText = DateTime.Today.ToString(StringResources.Current.DateFormatPattern);

    public BulkObservableCollection<CategoryStatItem> CategoryStats { get; } = new();
    public BulkObservableCollection<TopAppItem> TopApps { get; } = new();

    [ObservableProperty]
    private ISeries[] _pieChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _barChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _barChartXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _appTypePieChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private string _topCategoryName = StringResources.Current.NoData;

    [ObservableProperty]
    private string _topCategoryDuration = "--";

    // ========== 预算进度面板 ==========

    public BulkObservableCollection<BudgetProgressDisplayItem> BudgetProgressItems { get; } = new();

    [ObservableProperty]
    private int _totalBudgetCount = 0;

    [ObservableProperty]
    private int _completedBudgetCount = 0;

    [ObservableProperty]
    private int _overBudgetCount = 0;

    public bool HasBudgets => TotalBudgetCount > 0;
    public bool HasOverBudget => OverBudgetCount > 0;

    partial void OnTotalBudgetCountChanged(int value) => OnPropertyChanged(nameof(HasBudgets));
    partial void OnOverBudgetCountChanged(int value) => OnPropertyChanged(nameof(HasOverBudget));

    private bool _isMonitoring;
    public bool IsMonitoring
    {
        get => _isMonitoring;
        set => SetProperty(ref _isMonitoring, value);
    }

    [ObservableProperty]
    private string _monitoringStatusText = StringResources.Current.MonitoringNotStarted;

    [ObservableProperty]
    private string _startButtonText = StringResources.Current.StartMonitoring;

    private Func<Task>? _toggleMonitoringAction;

    public void SetToggleMonitoringAction(Func<Task> action)
    {
        _toggleMonitoringAction = action;
    }

    [RelayCommand]
    private async Task ToggleMonitoringAsync()
    {
        if (_toggleMonitoringAction != null)
            await _toggleMonitoringAction();
    }

    public DashboardViewModel(
        AppDataService appDataService,
        BudgetTrackingService budgetTrackingService,
        IIconExtractor iconExtractor)
    {
        _appDataService = appDataService;
        _budgetTrackingService = budgetTrackingService;
        _iconExtractor = iconExtractor;

        _budgetTrackingService.ProgressUpdated += OnBudgetProgressUpdated;

        _ = LoadDataForDateAsync(SelectedDate);
        _ = LoadBudgetProgressAsync();
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

    public async Task LoadDataForDateAsync(DateTime date)
    {
        _dashboardLoadCancellationTokenSource?.Cancel();
        _dashboardLoadCancellationTokenSource?.Dispose();
        _dashboardLoadCancellationTokenSource = new CancellationTokenSource();
        var loadToken = _dashboardLoadCancellationTokenSource.Token;

        _dashboardIconLoadCancellationTokenSource?.Cancel();
        _dashboardIconLoadCancellationTokenSource?.Dispose();
        _dashboardIconLoadCancellationTokenSource = new CancellationTokenSource();
        var iconToken = _dashboardIconLoadCancellationTokenSource.Token;

        await _loadDataLock.WaitAsync().ConfigureAwait(false);
        try
        {
            var snapshot = await _appDataService.GetSnapshotAsync(date).ConfigureAwait(false);

            if (loadToken.IsCancellationRequested) return;

            Log.Debug("Dashboard: 获取快照 [{DebugInfo}]", snapshot.GetDebugInfo());

            var currentFingerprint = $"{snapshot.SessionCount}_{snapshot.TotalSeconds}";
            if (currentFingerprint == _lastDataFingerprint)
            {
                Log.Debug("数据未变化,跳过UI更新");
                return;
            }

            _lastDataFingerprint = currentFingerprint;

            var dataUpdateTimeText = "--";
            if (IsMonitoring && date.Date == DateTime.Today)
            {
                dataUpdateTimeText = DateTime.Now.ToString("HH:mm");
            }
            else
            {
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
                _lastDataFingerprint = string.Empty;
                return;
            }

            var hours = snapshot.TotalSeconds / 3600;
            var minutes = (snapshot.TotalSeconds % 3600) / 60;
            var totalDurationText = $"{hours:D2}h {minutes:D2}m";

            var sessionCount = snapshot.SessionCount;
            var appTypeCount = snapshot.AllApps.Select(a => a.Category).Distinct().Count();

            await using var dbContext = new RecordTimeDbContext();
            var targetDate = date.Date;
            var activityTypeCount = await dbContext.Sessions
                .AsNoTracking()
                .Where(s => s.StartTime >= targetDate && s.StartTime < targetDate.AddDays(1))
                .Select(s => s.ActivityType)
                .Distinct()
                .CountAsync().ConfigureAwait(false);

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
                new SKColor(0x1A, 0x1A, 0x1A),
                new SKColor(0x4A, 0x4A, 0x4A),
                new SKColor(0x7A, 0x7A, 0x7A),
                new SKColor(0xA8, 0xA8, 0xA8),
                new SKColor(0xD0, 0xCF, 0xCB),
            };
            var pieChartData = top5Apps.Select((app, index) => new PieSeries<double>
            {
                Values = new[] { app.TotalDuration.TotalMinutes },
                Name = app.AppName,
                Fill = new SolidColorPaint(pieColors[index % pieColors.Length]),
                DataLabelsPaint = new SolidColorPaint(index == 0 ? new SKColor(0xF5, 0xF4, 0xF0) : new SKColor(0x1A, 0x1A, 0x1A)),
                DataLabelsSize = 12,
                DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F0}m"
            }).Cast<ISeries>().ToArray();

            var barGrays = new SKColor[]
            {
                new SKColor(0x1A, 0x1A, 0x1A),
                new SKColor(0x3D, 0x3D, 0x3D),
                new SKColor(0x5C, 0x5C, 0x5C),
                new SKColor(0x7A, 0x7A, 0x7A),
                new SKColor(0x99, 0x99, 0x99),
                new SKColor(0xB0, 0xB0, 0xB0),
                new SKColor(0xC8, 0xC8, 0xC8),
                new SKColor(0xD8, 0xD8, 0xD8),
            };

            ISeries[] barChartData;
            Axis[] barXAxes;
            if (categoryGroups.Count > 0)
            {
                var seriesList = new List<ISeries>();
                var categoryNames = new List<string>();
                for (int ci = 0; ci < categoryGroups.Count; ci++)
                {
                    var category = categoryGroups[ci];
                    var barColor = barGrays[ci % barGrays.Length];
                    categoryNames.Add(category.Category);
                    seriesList.Add(new ColumnSeries<double>
                    {
                        Values = new[] { category.Duration.TotalMinutes },
                        Name = category.Category,
                        Fill = new SolidColorPaint(barColor),
                        DataLabelsPaint = new SolidColorPaint(new SKColor(0x1A, 0x1A, 0x1A)),
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
                        LabelsPaint = new SolidColorPaint(new SKColor(0x6A, 0x6A, 0x6A)),
                        SeparatorsPaint = new SolidColorPaint(new SKColor(0xE2, 0xDF, 0xD6))
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
                var donutGrays = new[]
                {
                    new SKColor(0x1A, 0x1A, 0x1A),
                    new SKColor(0x4A, 0x4A, 0x4A),
                    new SKColor(0x7A, 0x7A, 0x7A),
                    new SKColor(0xA8, 0xA8, 0xA8),
                    new SKColor(0xD0, 0xCF, 0xCB),
                };
                donutData = appTypeGroups.Select((group, index) => new PieSeries<double>
                {
                    Values = new[] { group.TotalMinutes }, Name = group.Category,
                    Fill = new SolidColorPaint(donutGrays[index % donutGrays.Length]),
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

    private async Task LoadTopAppIconsAsync(
        List<(TopAppItem Item, string ProcessName, string Category)> requests,
        CancellationToken cancellationToken)
    {
        foreach (var (item, processName, category) in requests)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var icon = _iconExtractor.ExtractIcon(processName, category);
            if (icon == null) continue;

            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(
                () => item.Icon = icon,
                global::Avalonia.Threading.DispatcherPriority.Background);
        }
    }

    private async Task LoadBudgetProgressAsync()
    {
        try
        {
            var progressItems = await _budgetTrackingService.GetCurrentProgressAsync().ConfigureAwait(false);
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateBudgetProgressUI(progressItems));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载预算进度失败");
        }
    }

    private void OnBudgetProgressUpdated(object? sender, List<BudgetProgressItem> progressItems)
    {
        Log.Debug("Dashboard: 收到预算进度更新, {Count} 个预算", progressItems.Count);
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() => UpdateBudgetProgressUI(progressItems));
    }

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
    }

    private static string FormatProgressText(int actualMinutes, int targetMinutes)
    {
        var actualText = FormatDuration(actualMinutes);
        var targetText = FormatDuration(targetMinutes);
        return $"{actualText} / {targetText}";
    }

    private static string FormatStatusText(BudgetProgressItem item)
    {
        if (item.Budget.Type == BudgetType.Maximum)
        {
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
        _budgetTrackingService.ProgressUpdated -= OnBudgetProgressUpdated;

        _dashboardLoadCancellationTokenSource?.Cancel();
        _dashboardLoadCancellationTokenSource?.Dispose();
        _dashboardIconLoadCancellationTokenSource?.Cancel();
        _dashboardIconLoadCancellationTokenSource?.Dispose();

        _loadDataLock.Dispose();
    }
}
