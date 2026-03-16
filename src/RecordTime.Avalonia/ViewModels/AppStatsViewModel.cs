using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Avalonia.Resources.Strings;
using RecordTime.Avalonia.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

public partial class AppStatsViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _dateRangeText = "";

    [ObservableProperty]
    private string _totalDuration = "00h 00m";

    [ObservableProperty]
    private int _totalSessions = 0;

    [ObservableProperty]
    private int _totalAppCount = 0;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private string? _selectedCategory = null;

    [ObservableProperty]
    private bool _hasNoApps = true;

    // 应用列表
    public BulkObservableCollection<AppDetailItem> Apps { get; } = new();

    // 所有应用（用于过滤）
    private List<AppDetailItem> _allApps = new();

    // 分类列表
    public BulkObservableCollection<string> Categories { get; } = new();

    private readonly IIconExtractor _iconExtractor = new IconExtractor();
    private readonly AppDataService _appDataService;

    private CancellationTokenSource? _loadCancellationTokenSource;
    private CancellationTokenSource? _iconLoadCancellationTokenSource;

    public AppStatsViewModel(AppDataService appDataService)
    {
        _appDataService = appDataService;
        _dateRangeText = StringResources.Current.Today;
    }

    /// <summary>
    /// 页面激活时调用 - 每次导航到此页面时刷新数据
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
        await LoadDataAsync();
    }

    private System.Threading.Timer? _searchDebounceTimer;

    partial void OnSearchTextChanged(string value)
    {
        _searchDebounceTimer?.Dispose();
        _searchDebounceTimer = new System.Threading.Timer(
            _ => global::Avalonia.Threading.Dispatcher.UIThread.Post(FilterApps),
            null, dueTime: 150, period: Timeout.Infinite);
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        FilterApps();
    }

    private bool _suppressLoadOnDateChange = false;

    partial void OnStartDateChanged(DateTime value)
    {
        if (_suppressLoadOnDateChange) return;
        EndDate = value;
        DateRangeText = value.ToString("yyyy-MM-dd");
        _ = LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetTodayAsync()
    {
        _suppressLoadOnDateChange = true;
        StartDate = DateTime.Today;
        EndDate = DateTime.Today;
        _suppressLoadOnDateChange = false;
        DateRangeText = StringResources.Current.Today;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetYesterdayAsync()
    {
        _suppressLoadOnDateChange = true;
        StartDate = DateTime.Today.AddDays(-1);
        EndDate = DateTime.Today.AddDays(-1);
        _suppressLoadOnDateChange = false;
        DateRangeText = StringResources.Current.Yesterday;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetThisWeekAsync()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        if (dayOfWeek == 0) dayOfWeek = 7;
        var startOfWeek = today.AddDays(-(dayOfWeek - (int)DayOfWeek.Monday));
        _suppressLoadOnDateChange = true;
        StartDate = startOfWeek;
        EndDate = today;
        _suppressLoadOnDateChange = false;
        DateRangeText = StringResources.Current.ThisWeek;
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetThisMonthAsync()
    {
        var today = DateTime.Today;
        _suppressLoadOnDateChange = true;
        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = today;
        _suppressLoadOnDateChange = false;
        DateRangeText = StringResources.Current.ThisMonth;
        await LoadDataAsync();
    }

    [RelayCommand]
    private void ClearFilter()
    {
        SelectedCategory = null;
        SearchText = string.Empty;
    }

    private async Task LoadDataAsync()
    {
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _loadCancellationTokenSource = new CancellationTokenSource();

        _iconLoadCancellationTokenSource?.Cancel();
        _iconLoadCancellationTokenSource?.Dispose();
        _iconLoadCancellationTokenSource = new CancellationTokenSource();

        var loadToken = _loadCancellationTokenSource.Token;
        var iconToken = _iconLoadCancellationTokenSource.Token;

        try
        {
            var snapshot = await _appDataService.GetSnapshotAsync(StartDate, EndDate).ConfigureAwait(false);

            if (loadToken.IsCancellationRequested)
            {
                return;
            }

            Log.Debug("AppStats: 获取快照 [{DebugInfo}]", snapshot.GetDebugInfo());

            if (snapshot.AllApps.Count == 0)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TotalDuration = "00h 00m";
                    TotalSessions = 0;
                    TotalAppCount = 0;
                    _allApps = new List<AppDetailItem>();
                    Apps.ReplaceWith(Array.Empty<AppDetailItem>());
                    Categories.ReplaceWith(Array.Empty<string>());
                    HasNoApps = true;
                }, DispatcherPriority.Background);
                return;
            }

            // 计算汇总
            var hours = snapshot.TotalSeconds / 3600;
            var minutes = (snapshot.TotalSeconds % 3600) / 60;
            var durationText = $"{hours:D2}h {minutes:D2}m";

            // 转换为 AppDetailItem 并提取图标
            var appItems = snapshot.AllApps.Select(a => new AppDetailItem
            {
                AppName = a.AppName,
                ProcessName = a.ProcessName,
                Category = a.Category,
                TotalDuration = a.TotalDuration,
                SessionCount = a.SessionCount,
                FirstUsed = a.FirstUsed,
                LastUsed = a.LastUsed,
                TotalPercentage = a.TotalPercentage,
                Icon = _iconExtractor.ExtractIcon(string.Empty, a.Category)
            }).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalDuration = durationText;
                _allApps = appItems;
                TotalAppCount = _allApps.Count;
                TotalSessions = _allApps.Count;

                var allCategories = new List<string> { StringResources.Current.AllCategories };
                allCategories.AddRange(_allApps.Select(a => a.Category).Distinct().OrderBy(c => c));
                Categories.ReplaceWith(allCategories);

                Log.Debug("AppStats: 从快照获取了 {AppCount} 个应用", _allApps.Count);

                // 应用过滤
                FilterApps();
            }, DispatcherPriority.Background);

            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadIconsAsync(appItems, iconToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                }
            }, iconToken);

        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AppStats: 加载数据失败");
        }
    }

    private async Task LoadIconsAsync(List<AppDetailItem> items, CancellationToken cancellationToken)
    {
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var icon = _iconExtractor.ExtractIcon(item.ProcessName, item.Category);
            if (icon == null)
            {
                continue;
            }

            await Dispatcher.UIThread.InvokeAsync(() => item.Icon = icon, DispatcherPriority.Background);
        }
    }

    public void Dispose()
    {
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _iconLoadCancellationTokenSource?.Cancel();
        _iconLoadCancellationTokenSource?.Dispose();
        _searchDebounceTimer?.Dispose();
    }

    private void FilterApps()
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(FilterApps);
            return;
        }

        var filtered = _allApps.AsEnumerable();

        // 分类过滤
        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != StringResources.Current.AllCategories)
        {
            filtered = filtered.Where(a => a.Category == SelectedCategory);
        }

        // 搜索过滤
        if (!string.IsNullOrEmpty(SearchText))
        {
            filtered = filtered.Where(a =>
                a.AppName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                a.ProcessName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
        }

        var filteredList = filtered.ToList();

        Apps.ReplaceWith(filteredList);

        TotalSessions = filteredList.Count;
        HasNoApps = filteredList.Count == 0;
    }
}
