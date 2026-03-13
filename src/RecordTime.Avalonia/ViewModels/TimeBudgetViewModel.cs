using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Core.Models;
using RecordTime.Data;
using RecordTime.Data.Repositories;
using RecordTime.Avalonia.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

/// <summary>
/// 时间目标项 - 用于 UI 显示
/// </summary>
public partial class TimeBudgetItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _targetType = string.Empty;

    [ObservableProperty]
    private BudgetType _budgetType;

    [ObservableProperty]
    private int _targetMinutes;

    [ObservableProperty]
    private bool _isEnabled = true;

    [ObservableProperty]
    private bool _reminderEnabled = true;

    [ObservableProperty]
    private int _reminderThreshold = 80;

    [ObservableProperty]
    private bool _isAppBudget;

    [ObservableProperty]
    private string? _processName;

    [ObservableProperty]
    private string? _category;

    // 进度追踪属性
    [ObservableProperty]
    private int _actualMinutes;

    [ObservableProperty]
    private double _progressPercentage;

    [ObservableProperty]
    private bool _isOverBudget;

    [ObservableProperty]
    private bool _isGoalMet;

    [ObservableProperty]
    private string _progressStatusText = string.Empty;

    /// <summary>
    /// 是否显示"目标已达成"消息
    /// 仅对下限(Minimum)类型预算且已达成目标时显示
    /// 上限(Maximum)类型不应显示达成消息，因为"未超标"不等于"达成目标"
    /// </summary>
    public bool ShowGoalMetMessage => BudgetType == BudgetType.Minimum && IsGoalMet;

    /// <summary>
    /// 格式化的目标时长
    /// </summary>
    public string TargetDurationText
    {
        get
        {
            var hours = TargetMinutes / 60;
            var minutes = TargetMinutes % 60;
            if (hours > 0 && minutes > 0)
                return $"{hours}小时{minutes}分钟";
            else if (hours > 0)
                return $"{hours}小时";
            else
                return $"{minutes}分钟";
        }
    }

    /// <summary>
    /// 格式化的实际使用时长
    /// </summary>
    public string ActualDurationText
    {
        get
        {
            var hours = ActualMinutes / 60;
            var minutes = ActualMinutes % 60;
            if (hours > 0 && minutes > 0)
                return $"{hours}h {minutes}m";
            else if (hours > 0)
                return $"{hours}h";
            else
                return $"{minutes}m";
        }
    }

    /// <summary>
    /// 进度条颜色
    /// </summary>
    public IBrush ProgressColorBrush
    {
        get
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
            return new SolidColorBrush(Color.Parse(colorHex));
        }
    }

    /// <summary>
    /// 目标类型标签
    /// </summary>
    public string TypeLabel => BudgetType == BudgetType.Maximum ? "上限" : "下限";

    /// <summary>
    /// 目标类型颜色
    /// </summary>
    public string TypeColor => BudgetType == BudgetType.Maximum ? "#FF6B6B" : "#4ECDC4";

    /// <summary>
    /// 从 BudgetProgressItem 创建（包含进度信息）
    /// </summary>
    public static TimeBudgetItem FromProgressItem(BudgetProgressItem progressItem)
    {
        var item = new TimeBudgetItem
        {
            Id = progressItem.Budget.Id,
            DisplayName = progressItem.Budget.DisplayName,
            BudgetType = progressItem.Budget.Type,
            TargetType = progressItem.Budget.Type == BudgetType.Maximum ? "上限目标" : "下限目标",
            TargetMinutes = progressItem.Budget.TargetMinutes,
            IsEnabled = progressItem.Budget.IsEnabled,
            ReminderEnabled = progressItem.Budget.ReminderEnabled,
            ReminderThreshold = progressItem.Budget.ReminderThreshold,
            IsAppBudget = progressItem.Budget.IsAppBudget,
            ProcessName = progressItem.Budget.ProcessName,
            Category = progressItem.Budget.Category,
            // 进度信息
            ActualMinutes = progressItem.TodayActualMinutes,
            ProgressPercentage = progressItem.ProgressPercentage,
            IsOverBudget = progressItem.IsOverBudget,
            IsGoalMet = progressItem.IsGoalMet,
            ProgressStatusText = progressItem.StatusText
        };
        return item;
    }

    /// <summary>
    /// 从 TimeBudget 模型创建
    /// </summary>
    public static TimeBudgetItem FromModel(TimeBudget budget)
    {
        return new TimeBudgetItem
        {
            Id = budget.Id,
            DisplayName = budget.DisplayName,
            BudgetType = budget.Type,
            TargetType = budget.Type == BudgetType.Maximum ? "上限目标" : "下限目标",
            TargetMinutes = budget.TargetMinutes,
            IsEnabled = budget.IsEnabled,
            ReminderEnabled = budget.ReminderEnabled,
            ReminderThreshold = budget.ReminderThreshold,
            IsAppBudget = budget.IsAppBudget,
            ProcessName = budget.ProcessName,
            Category = budget.Category
        };
    }

    /// <summary>
    /// 转换为 TimeBudget 模型
    /// </summary>
    public TimeBudget ToModel()
    {
        return new TimeBudget
        {
            Id = Id,
            DisplayName = DisplayName,
            Type = BudgetType,
            TargetMinutes = TargetMinutes,
            IsEnabled = IsEnabled,
            ReminderEnabled = ReminderEnabled,
            ReminderThreshold = ReminderThreshold,
            ProcessName = IsAppBudget ? ProcessName : null,
            Category = IsAppBudget ? null : Category
        };
    }
}

/// <summary>
/// 建议项 - 用于 UI 显示
/// </summary>
public partial class SuggestionItem : ObservableObject
{
    [ObservableProperty]
    private int _id;

    [ObservableProperty]
    private string _displayName = string.Empty;

    [ObservableProperty]
    private string _reason = string.Empty;

    [ObservableProperty]
    private BudgetType _suggestedType;

    [ObservableProperty]
    private int _suggestedMinutes;

    [ObservableProperty]
    private int _historicalMinutes;

    [ObservableProperty]
    private int _priority;

    [ObservableProperty]
    private bool _isAppSuggestion;

    [ObservableProperty]
    private string? _processName;

    [ObservableProperty]
    private string? _category;

    public string TypeLabel => SuggestedType == BudgetType.Maximum ? "上限" : "下限";

    public string SuggestedTimeText
    {
        get
        {
            var hours = SuggestedMinutes / 60;
            var minutes = SuggestedMinutes % 60;
            if (hours > 0 && minutes > 0)
                return $"{hours}小时{minutes}分钟";
            else if (hours > 0)
                return $"{hours}小时";
            else
                return $"{minutes}分钟";
        }
    }

    public string HistoricalTimeText
    {
        get
        {
            var hours = HistoricalMinutes / 60;
            var minutes = HistoricalMinutes % 60;
            if (hours > 0)
                return $"历史平均: {hours}h{minutes}m/天";
            else
                return $"历史平均: {minutes}分钟/天";
        }
    }

    public static SuggestionItem FromModel(GoalSuggestion suggestion)
    {
        return new SuggestionItem
        {
            Id = suggestion.Id,
            DisplayName = suggestion.DisplayName,
            Reason = suggestion.Reason,
            SuggestedType = suggestion.SuggestedType,
            SuggestedMinutes = suggestion.SuggestedMinutes,
            HistoricalMinutes = suggestion.HistoricalAverageMinutes,
            Priority = suggestion.Priority,
            IsAppSuggestion = suggestion.IsAppSuggestion,
            ProcessName = suggestion.ProcessName,
            Category = suggestion.Category
        };
    }
}

/// <summary>
/// 时间目标设置页面 ViewModel
/// </summary>
public partial class TimeBudgetViewModel : ViewModelBase
{
    /// <summary>
    /// 所有时间目标
    /// </summary>
    public BulkObservableCollection<TimeBudgetItem> Budgets { get; } = new();

    /// <summary>
    /// AI 建议列表
    /// </summary>
    public BulkObservableCollection<SuggestionItem> Suggestions { get; } = new();

    /// <summary>
    /// 可用的应用列表（用于选择）- 存储 ProcessName 和 DisplayName 的映射
    /// </summary>
    private Dictionary<string, string> _appDisplayNameToProcessNameMap = new();

    /// <summary>
    /// 可用的应用列表（用于下拉框显示）
    /// </summary>
    public BulkObservableCollection<string> AvailableApps { get; } = new();

    /// <summary>
    /// 可用的分类列表（用于选择）
    /// </summary>
    public BulkObservableCollection<string> AvailableCategories { get; } = new();

    [ObservableProperty]
    private bool _isLoading = false;

    [ObservableProperty]
    private string _statusMessage = "准备就绪";

    [ObservableProperty]
    private bool _hasBudgets = false;

    [ObservableProperty]
    private bool _hasSuggestions = false;

    [ObservableProperty]
    private bool _isGeneratingSuggestions = false;

    // 编辑状态
    [ObservableProperty]
    private bool _isEditing = false;

    [ObservableProperty]
    private TimeBudgetItem? _editingBudget;

    // 新增/编辑表单字段
    [ObservableProperty]
    private string _editDisplayName = string.Empty;

    [ObservableProperty]
    private bool _editIsAppBudget = true;

    [ObservableProperty]
    private string? _editProcessName;

    [ObservableProperty]
    private string? _editCategory;

    [ObservableProperty]
    private int _editBudgetTypeIndex = 0; // 0 = Maximum, 1 = Minimum

    /// <summary>
    /// 是否为上限目标 (用于 RadioButton 绑定)
    /// </summary>
    [ObservableProperty]
    private bool _editIsMaximum = true;

    /// <summary>
    /// 是否为下限目标 (用于 RadioButton 绑定)
    /// </summary>
    public bool EditIsMinimum
    {
        get => !EditIsMaximum;
        set => EditIsMaximum = !value;
    }

    partial void OnEditIsMaximumChanged(bool value)
    {
        EditBudgetTypeIndex = value ? 0 : 1;
        OnPropertyChanged(nameof(EditIsMinimum));
    }

    [ObservableProperty]
    private int _editTargetHours = 1;

    [ObservableProperty]
    private int _editTargetMinutes = 0;

    [ObservableProperty]
    private bool _editReminderEnabled = true;

    [ObservableProperty]
    private int _editReminderThreshold = 80;

    private readonly GoalSuggestionEngine _suggestionEngine = new();
    private readonly BudgetTrackingService _trackingService = BudgetTrackingService.Instance;

    private CancellationTokenSource? _loadCancellationTokenSource;

    public TimeBudgetViewModel()
    {
        // 订阅预算进度更新事件
        _trackingService.ProgressUpdated += OnProgressUpdated;

    }

    /// <summary>
    /// 页面导航时调用，刷新进度数据
    /// </summary>
    public async Task OnNavigatedToAsync()
    {
        // 如果监控正在运行，立即触发一次进度更新
        if (_trackingService.IsRunning)
        {
            await _trackingService.UpdateProgressAsync();
        }
        else
        {
            // 监控未运行时，只刷新数据显示
            await LoadDataAsync();
        }
    }

    /// <summary>
    /// 处理预算进度更新事件
    /// </summary>
    private void OnProgressUpdated(object? sender, List<BudgetProgressItem> progressItems)
    {
        Log.Debug("收到预算进度更新,共 {Count} 个预算", progressItems.Count);

        // 在 UI 线程上更新进度
        global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            // 更新已启用的预算进度
            foreach (var progressItem in progressItems)
            {
                var existingBudget = Budgets.FirstOrDefault(b => b.Id == progressItem.Budget.Id);
                if (existingBudget != null)
                {
                    existingBudget.ActualMinutes = progressItem.TodayActualMinutes;
                    existingBudget.ProgressPercentage = progressItem.ProgressPercentage;
                    existingBudget.IsOverBudget = progressItem.IsOverBudget;
                    existingBudget.IsGoalMet = progressItem.IsGoalMet;
                    existingBudget.ProgressStatusText = progressItem.StatusText;

                    Log.Debug("更新预算 {DisplayName}: {ActualMinutes}/{TargetMinutes} 分钟 ({Progress:F1}%)",
                        existingBudget.DisplayName,
                        existingBudget.ActualMinutes,
                        existingBudget.TargetMinutes,
                        existingBudget.ProgressPercentage);
                }
            }
        });
    }

    /// <summary>
    /// 加载所有数据
    /// </summary>
    [RelayCommand]
    public async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "正在加载...";

        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _loadCancellationTokenSource = new CancellationTokenSource();
        var loadToken = _loadCancellationTokenSource.Token;

        try
        {
            await using var context = new RecordTimeDbContext();

            // 使用 BudgetTrackingService 获取带进度的预算数据
            var progressItems = await _trackingService.GetCurrentProgressAsync().ConfigureAwait(false);
            if (loadToken.IsCancellationRequested)
            {
                return;
            }
            var budgetItems = progressItems.Select(TimeBudgetItem.FromProgressItem).ToList();

            // 也加载未启用的预算（不显示进度）
            var disabledBudgets = await context.TimeBudgets
                .AsNoTracking()
                .Where(b => !b.IsEnabled)
                .OrderBy(b => b.DisplayName)
                .ToListAsync()
                .ConfigureAwait(false);

            budgetItems.AddRange(disabledBudgets.Select(TimeBudgetItem.FromModel));

            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                Budgets.ReplaceWith(budgetItems);
                HasBudgets = Budgets.Count > 0;
                StatusMessage = $"已加载 {budgetItems.Count} 个时间目标";
            }, global::Avalonia.Threading.DispatcherPriority.Background);

            // 加载可用应用和分类
            if (loadToken.IsCancellationRequested)
            {
                return;
            }

            await LoadAvailableOptionsAsync(context);

            // 加载建议（如果有）
            if (loadToken.IsCancellationRequested)
            {
                return;
            }

            await LoadSuggestionsAsync(context);

        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载时间目标失败");
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => StatusMessage = "加载失败: " + ex.Message);
        }
        finally
        {
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() => IsLoading = false);
        }
    }

    /// <summary>
    /// 加载可用的应用和分类选项
    /// </summary>
    private async Task LoadAvailableOptionsAsync(RecordTimeDbContext context)
    {
        // 获取最近 30 天使用过的应用
        var thirtyDaysAgo = DateTime.Today.AddDays(-30);

        var apps = await context.Sessions
            .AsNoTracking()
            .Where(s => s.StartTime >= thirtyDaysAgo)
            .Select(s => new { s.ProcessName, s.DisplayName })
            .Distinct()
            .OrderBy(a => a.DisplayName)
            .ToListAsync()
            .ConfigureAwait(false);

        var appNames = apps.Select(a => a.DisplayName).ToList();
        var appMap = apps
            .GroupBy(a => a.DisplayName)
            .ToDictionary(g => g.Key, g => g.First().ProcessName);

        // 获取所有分类
        var categories = await context.Sessions
            .AsNoTracking()
            .Where(s => s.StartTime >= thirtyDaysAgo && s.Category != null)
            .Select(s => s.Category!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync()
            .ConfigureAwait(false);

        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            AvailableApps.ReplaceWith(appNames);
            _appDisplayNameToProcessNameMap = appMap;
            AvailableCategories.ReplaceWith(categories);
        }, global::Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// 加载已有的建议
    /// </summary>
    private async Task LoadSuggestionsAsync(RecordTimeDbContext context)
    {
        var suggestions = await context.GoalSuggestions
            .AsNoTracking()
            .Where(s => !s.IsProcessed && s.ExpiresAt > DateTime.Now)
            .OrderByDescending(s => s.Priority)
            .ToListAsync()
            .ConfigureAwait(false);

        var suggestionItems = suggestions.Select(SuggestionItem.FromModel).ToList();

        await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
        {
            Suggestions.ReplaceWith(suggestionItems);
            HasSuggestions = Suggestions.Count > 0;
        }, global::Avalonia.Threading.DispatcherPriority.Background);
    }

    /// <summary>
    /// 生成 AI 建议
    /// </summary>
    [RelayCommand]
    private async Task GenerateSuggestionsAsync()
    {
        if (IsGeneratingSuggestions)
            return;

        IsGeneratingSuggestions = true;
        StatusMessage = "正在分析使用模式...";

        try
        {
            var suggestions = await _suggestionEngine.GenerateSuggestionsAsync(14);

            if (suggestions.Count == 0)
            {
                StatusMessage = "暂无新建议，请继续使用应用积累数据";
                return;
            }

            // 保存建议到数据库
            await using var context = new RecordTimeDbContext();
            foreach (var suggestion in suggestions)
            {
                context.GoalSuggestions.Add(suggestion);
            }
            await context.SaveChangesAsync();

            // 刷新 UI
            Suggestions.Clear();
            foreach (var suggestion in suggestions)
            {
                Suggestions.Add(SuggestionItem.FromModel(suggestion));
            }

            HasSuggestions = true;
            StatusMessage = $"已生成 {suggestions.Count} 条建议";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "生成建议失败");
            StatusMessage = "生成建议失败: " + ex.Message;
        }
        finally
        {
            IsGeneratingSuggestions = false;
        }
    }

    /// <summary>
    /// 接受建议
    /// </summary>
    [RelayCommand]
    private async Task AcceptSuggestionAsync(SuggestionItem suggestion)
    {
        try
        {
            await using var context = new RecordTimeDbContext();

            // 创建新的预算
            var budget = new TimeBudget
            {
                DisplayName = suggestion.DisplayName,
                Type = suggestion.SuggestedType,
                TargetMinutes = suggestion.SuggestedMinutes,
                ProcessName = suggestion.ProcessName,
                Category = suggestion.Category,
                IsEnabled = true,
                ReminderEnabled = true,
                ReminderThreshold = 80
            };

            context.TimeBudgets.Add(budget);

            // 标记建议为已处理
            var dbSuggestion = await context.GoalSuggestions.FindAsync(suggestion.Id);
            if (dbSuggestion != null)
            {
                dbSuggestion.IsProcessed = true;
                dbSuggestion.ProcessResult = "Accepted";
            }

            await context.SaveChangesAsync();

            // 更新 UI
            Budgets.Add(TimeBudgetItem.FromModel(budget));
            Suggestions.Remove(suggestion);

            HasBudgets = true;
            HasSuggestions = Suggestions.Count > 0;
            StatusMessage = $"已添加目标: {budget.DisplayName}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "接受建议失败");
            StatusMessage = "接受建议失败: " + ex.Message;
        }
    }

    /// <summary>
    /// 忽略建议
    /// </summary>
    [RelayCommand]
    private async Task IgnoreSuggestionAsync(SuggestionItem suggestion)
    {
        try
        {
            await using var context = new RecordTimeDbContext();

            var dbSuggestion = await context.GoalSuggestions.FindAsync(suggestion.Id);
            if (dbSuggestion != null)
            {
                dbSuggestion.IsProcessed = true;
                dbSuggestion.ProcessResult = "Ignored";
                await context.SaveChangesAsync();
            }

            Suggestions.Remove(suggestion);
            HasSuggestions = Suggestions.Count > 0;
            StatusMessage = "已忽略建议";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "忽略建议失败");
            StatusMessage = "忽略建议失败: " + ex.Message;
        }
    }

    /// <summary>
    /// 开始新增目标
    /// </summary>
    [RelayCommand]
    private async Task StartAddBudgetAsync()
    {
        // 刷新可用应用列表
        await using var context = new RecordTimeDbContext();
        await LoadAvailableOptionsAsync(context);

        EditingBudget = null;
        EditDisplayName = string.Empty;
        EditIsAppBudget = true;
        EditProcessName = null;
        EditCategory = null;
        EditBudgetTypeIndex = 0;
        EditIsMaximum = true;
        EditTargetHours = 1;
        EditTargetMinutes = 0;
        EditReminderEnabled = true;
        EditReminderThreshold = 80;
        IsEditing = true;
    }

    /// <summary>
    /// 开始编辑目标
    /// </summary>
    [RelayCommand]
    private async Task StartEditBudgetAsync(TimeBudgetItem budget)
    {
        // 刷新可用应用列表
        await using var context = new RecordTimeDbContext();
        await LoadAvailableOptionsAsync(context);

        EditingBudget = budget;
        EditDisplayName = budget.DisplayName;
        EditIsAppBudget = budget.IsAppBudget;

        // 如果是应用预算，需要从 ProcessName 找到对应的 DisplayName
        if (budget.IsAppBudget && !string.IsNullOrEmpty(budget.ProcessName))
        {
            // 在映射中查找对应的 DisplayName
            var displayName = _appDisplayNameToProcessNameMap
                .FirstOrDefault(kvp => kvp.Value == budget.ProcessName).Key;
            EditProcessName = displayName ?? budget.ProcessName;
        }
        else
        {
            EditProcessName = budget.ProcessName;
        }

        EditCategory = budget.Category;
        EditBudgetTypeIndex = budget.BudgetType == BudgetType.Maximum ? 0 : 1;
        EditIsMaximum = budget.BudgetType == BudgetType.Maximum;
        EditTargetHours = budget.TargetMinutes / 60;
        EditTargetMinutes = budget.TargetMinutes % 60;
        EditReminderEnabled = budget.ReminderEnabled;
        EditReminderThreshold = budget.ReminderThreshold;
        IsEditing = true;
    }

    /// <summary>
    /// 取消编辑
    /// </summary>
    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing = false;
        EditingBudget = null;
    }

    /// <summary>
    /// 保存目标
    /// </summary>
    [RelayCommand]
    private async Task SaveBudgetAsync()
    {
        if (string.IsNullOrWhiteSpace(EditDisplayName))
        {
            StatusMessage = "请输入目标名称";
            return;
        }

        var totalMinutes = EditTargetHours * 60 + EditTargetMinutes;
        if (totalMinutes <= 0)
        {
            StatusMessage = "请设置有效的目标时长";
            return;
        }

        // 在清空 EditingBudget 之前记录编辑状态
        var isEditingExisting = EditingBudget != null;

        try
        {
            await using var context = new RecordTimeDbContext();

            TimeBudget budget;
            if (EditingBudget != null)
            {
                // 编辑现有
                budget = await context.TimeBudgets.FindAsync(EditingBudget.Id) ?? new TimeBudget();
            }
            else
            {
                // 新增
                budget = new TimeBudget();
                context.TimeBudgets.Add(budget);
            }

            budget.DisplayName = EditDisplayName;
            budget.Type = EditBudgetTypeIndex == 0 ? BudgetType.Maximum : BudgetType.Minimum;
            budget.TargetMinutes = totalMinutes;

            // 保存时将 DisplayName 转换为 ProcessName
            if (EditIsAppBudget)
            {
                // 如果用户选择了DisplayName,转换为ProcessName
                if (_appDisplayNameToProcessNameMap.TryGetValue(EditProcessName ?? "", out var processName))
                {
                    budget.ProcessName = processName;
                }
                else
                {
                    // 如果找不到映射，可能是直接输入的ProcessName，直接使用
                    budget.ProcessName = EditProcessName;
                }
                budget.Category = null;
            }
            else
            {
                budget.ProcessName = null;
                budget.Category = EditCategory;
            }

            budget.ReminderEnabled = EditReminderEnabled;
            budget.ReminderThreshold = EditReminderThreshold;
            budget.IsEnabled = true;
            budget.UpdatedAt = DateTime.Now;

            await context.SaveChangesAsync();

            // 刷新列表
            await LoadDataAsync();

            IsEditing = false;
            EditingBudget = null;
            StatusMessage = isEditingExisting
                ? Resources.Strings.StringResources.Current.BudgetUpdatedMessage
                : Resources.Strings.StringResources.Current.BudgetAddedMessage;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存目标失败");
            StatusMessage = "保存失败: " + ex.Message;
        }
    }

    /// <summary>
    /// 删除目标
    /// </summary>
    [RelayCommand]
    private async Task DeleteBudgetAsync(TimeBudgetItem budget)
    {
        try
        {
            await using var context = new RecordTimeDbContext();

            var dbBudget = await context.TimeBudgets.FindAsync(budget.Id);
            if (dbBudget != null)
            {
                context.TimeBudgets.Remove(dbBudget);
                await context.SaveChangesAsync();
            }

            Budgets.Remove(budget);
            HasBudgets = Budgets.Count > 0;
            StatusMessage = $"已删除目标: {budget.DisplayName}";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "删除目标失败");
            StatusMessage = "删除失败: " + ex.Message;
        }
    }

    /// <summary>
    /// 切换目标启用状态
    /// </summary>
    [RelayCommand]
    private async Task ToggleBudgetEnabledAsync(TimeBudgetItem budget)
    {
        try
        {
            await using var context = new RecordTimeDbContext();

            var dbBudget = await context.TimeBudgets.FindAsync(budget.Id);
            if (dbBudget != null)
            {
                dbBudget.IsEnabled = !dbBudget.IsEnabled;
                dbBudget.UpdatedAt = DateTime.Now;
                await context.SaveChangesAsync();

                budget.IsEnabled = dbBudget.IsEnabled;
                StatusMessage = dbBudget.IsEnabled ? $"已启用: {budget.DisplayName}" : $"已禁用: {budget.DisplayName}";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "切换目标状态失败");
            StatusMessage = "操作失败: " + ex.Message;
        }
    }
}
