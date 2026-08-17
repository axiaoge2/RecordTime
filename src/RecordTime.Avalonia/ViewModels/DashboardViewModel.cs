using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Core.Models;
using RecordTime.Core.Services;
using RecordTime.Avalonia.Services;
using RecordTime.Avalonia.Resources.Strings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

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

    public BulkObservableCollection<TopAppItem> TopApps { get; } = new();

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

        _ = LoadDataForDateAsync(SelectedDate).ContinueWith(t =>
            Log.Error(t.Exception, "Dashboard 初始化加载数据失败"), TaskContinuationOptions.OnlyOnFaulted);
        _ = LoadBudgetProgressAsync().ContinueWith(t =>
            Log.Error(t.Exception, "Dashboard 初始化加载预算进度失败"), TaskContinuationOptions.OnlyOnFaulted);
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

            var currentFingerprint = $"{snapshot.SessionCount}_{snapshot.TotalSeconds}_{snapshot.AllApps.Count}_{snapshot.AllApps.FirstOrDefault()?.TotalDuration}";
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
                var lastSessionEnd = await _appDataService.GetLastSessionEndTimeAsync(date).ConfigureAwait(false);
                dataUpdateTimeText = lastSessionEnd.HasValue ? lastSessionEnd.Value.ToString("HH:mm") : "--";
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
                    TopApps.Clear();
                }, global::Avalonia.Threading.DispatcherPriority.Background);
                _lastDataFingerprint = string.Empty;
                return;
            }

            var hours = snapshot.TotalSeconds / 3600;
            var minutes = (snapshot.TotalSeconds % 3600) / 60;
            var totalDurationText = $"{hours:D2}h {minutes:D2}m";

            var sessionCount = snapshot.SessionCount;
            var appTypeCount = snapshot.AllApps.Select(a => a.Category).Distinct().Count();
            var activityTypeCount = await _appDataService.GetActivityTypeCountAsync(date).ConfigureAwait(false);

            var top10Apps = snapshot.AllApps.Take(10).ToList();
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

            if (loadToken.IsCancellationRequested) return;

            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                DataUpdateTime = dataUpdateTimeText;
                ShowEmptyState = false;
                TotalDuration = totalDurationText;
                SessionCount = sessionCount;
                AppTypeCount = appTypeCount;
                ActivityTypeCount = activityTypeCount;
                TopApps.ReplaceWith(topAppItems);
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
