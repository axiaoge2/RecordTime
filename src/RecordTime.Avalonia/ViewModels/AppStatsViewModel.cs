using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Avalonia.Services;
using RecordTime.Core.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace RecordTime.Avalonia.ViewModels;

public partial class AppStatsViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _dateRangeText = "今日";

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
    public ObservableCollection<AppDetailItem> Apps { get; } = new();

    // 所有应用（用于过滤）
    private List<AppDetailItem> _allApps = new();

    // 分类列表
    public ObservableCollection<string> Categories { get; } = new();

    public AppStatsViewModel()
    {
        _ = LoadDataAsync();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterApps();
    }

    partial void OnSelectedCategoryChanged(string? value)
    {
        FilterApps();
    }

    [RelayCommand]
    private async Task SetTodayAsync()
    {
        StartDate = DateTime.Today;
        EndDate = DateTime.Today;
        DateRangeText = "今日";
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetYesterdayAsync()
    {
        StartDate = DateTime.Today.AddDays(-1);
        EndDate = DateTime.Today.AddDays(-1);
        DateRangeText = "昨日";
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetThisWeekAsync()
    {
        var today = DateTime.Today;
        var dayOfWeek = (int)today.DayOfWeek;
        if (dayOfWeek == 0)
        {
            dayOfWeek = 7;
        }
        var startOfWeek = today.AddDays(-(dayOfWeek - (int)DayOfWeek.Monday));
        StartDate = startOfWeek;
        EndDate = today;
        DateRangeText = "本周";
        await LoadDataAsync();
    }

    [RelayCommand]
    private async Task SetThisMonthAsync()
    {
        var today = DateTime.Today;
        StartDate = new DateTime(today.Year, today.Month, 1);
        EndDate = today;
        DateRangeText = "本月";
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
        try
        {
            // 使用共享的 AppDataService 获取快照
            var appDataService = AppDataService.Instance;
            var snapshot = await appDataService.GetSnapshotAsync(StartDate, EndDate);

            System.Diagnostics.Debug.WriteLine($"=== AppStatsViewModel: 获取快照 [{snapshot.GetDebugInfo()}] ===");

            if (snapshot.AllApps.Count == 0)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    TotalDuration = "00h 00m";
                    TotalSessions = 0;
                    TotalAppCount = 0;
                    _allApps = new List<AppDetailItem>();
                    Apps.Clear();
                    Categories.Clear();
                    HasNoApps = true;
                });
                return;
            }

            // 计算汇总
            var hours = snapshot.TotalSeconds / 3600;
            var minutes = (snapshot.TotalSeconds % 3600) / 60;
            var durationText = $"{hours:D2}h {minutes:D2}m";

            // 转换为 AppDetailItem
            var appItems = snapshot.AllApps.Select(a => new AppDetailItem
            {
                AppName = a.AppName,
                ProcessName = a.ProcessName,
                Category = a.Category,
                TotalDuration = a.TotalDuration,
                SessionCount = a.SessionCount,
                FirstUsed = a.FirstUsed,
                LastUsed = a.LastUsed,
                TotalPercentage = a.TotalPercentage
            }).ToList();

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                TotalDuration = durationText;
                _allApps = appItems;
                TotalAppCount = _allApps.Count;
                TotalSessions = _allApps.Count;

                Categories.Clear();
                Categories.Add("全部分类");
                var categories = _allApps.Select(a => a.Category).Distinct().OrderBy(c => c);
                foreach (var category in categories)
                {
                    Categories.Add(category);
                }

                System.Diagnostics.Debug.WriteLine($"=== AppStatsViewModel: 从快照获取了 {_allApps.Count} 个应用 ===");

                // 应用过滤
                FilterApps();
            });

        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载数据失败: {ex.Message}");
        }
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
        if (!string.IsNullOrEmpty(SelectedCategory) && SelectedCategory != "全部分类")
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

        Apps.Clear();
        foreach (var app in filteredList)
        {
            Apps.Add(app);
        }

        TotalSessions = filteredList.Count;
        HasNoApps = filteredList.Count == 0;
    }
}

// 应用详细项
public partial class AppDetailItem : ObservableObject
{
    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _processName = string.Empty;

    [ObservableProperty]
    private string _category = string.Empty;

    [ObservableProperty]
    private TimeSpan _totalDuration;

    [ObservableProperty]
    private int _sessionCount;

    [ObservableProperty]
    private DateTime _firstUsed;

    [ObservableProperty]
    private DateTime _lastUsed;

    [ObservableProperty]
    private double _totalPercentage;

    public string DurationText => $"{(int)TotalDuration.TotalHours:D2}:{TotalDuration.Minutes:D2}:{TotalDuration.Seconds:D2}";
    public string SessionCountText => $"{SessionCount} 次";
    public string PercentageText => $"{TotalPercentage:F1}%";
    public string FirstUsedText => FirstUsed.ToString("HH:mm:ss");
    public string LastUsedText => LastUsed.ToString("HH:mm:ss");
}
