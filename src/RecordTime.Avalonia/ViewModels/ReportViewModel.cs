using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Avalonia.Resources.Strings;
using RecordTime.Data;
using RecordTime.Data.Repositories;
using RecordTime.Data.Reports;
using RecordTime.Core.Models;
using RecordTime.Core.Models.AI;
using RecordTime.Core.Services.AI;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

public partial class ReportViewModel : ViewModelBase, IDisposable
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _startDateText = DateTime.Today.AddDays(-7).ToString(StringResources.Current.DateFormatPattern);

    [ObservableProperty]
    private string _endDateText = DateTime.Today.ToString(StringResources.Current.DateFormatPattern);

    [ObservableProperty]
    private string _statusText = StringResources.Current.ReportReadyStatus;

    [ObservableProperty]
    private string? _statusDetail;

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private bool _isCustomDateMode = false;

    [ObservableProperty]
    private string? _lastReportPath;

    [ObservableProperty]
    private int _totalDays = 0;

    [ObservableProperty]
    private int _totalSessions = 0;

    [ObservableProperty]
    private int _totalApps = 0;

    // LiveCharts 图表属性

    // 7天趋势折线图
    [ObservableProperty]
    private ISeries[] _weeklyTrendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _weeklyTrendXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _weeklyTrendYAxes = Array.Empty<Axis>();

    // 活动类型分布饼图
    [ObservableProperty]
    private ISeries[] _activityDistributionSeries = Array.Empty<ISeries>();

    // AI 相关属性
    [ObservableProperty]
    private bool _enableAIAnalysis = false;

    [ObservableProperty]
    private bool _isAIConfigured = false;

    [ObservableProperty]
    private string _aiStatusText = StringResources.Current.AINotConfigured;

    [ObservableProperty]
    private string _aiApiKey = string.Empty;

    [ObservableProperty]
    private string _aiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _aiBaseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private AIPrivacyLevel _aiPrivacyLevel = AIPrivacyLevel.CategoryOnly;

    [ObservableProperty]
    private string _currentConfigName = StringResources.Current.DefaultConfigName;

    [ObservableProperty]
    private List<string> _availableConfigs = new();

    private string _originalConfigName = StringResources.Current.DefaultConfigName; // 用于跟踪配置重命名

    [ObservableProperty]
    private bool _isTestingConnection = false;

    [ObservableProperty]
    private string _connectionTestIcon = "🔗";

    private readonly AIConfigManager _configManager = new();
    private CancellationTokenSource? _aiCancellationTokenSource;
    private CancellationTokenSource? _chartLoadCancellationTokenSource;
    private System.Timers.Timer? _chartLoadDebounceTimer;

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime CacheTime, List<DailyUsage> Data)> _trendCache = new();
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, (DateTime CacheTime, Dictionary<ActivityType, TimeSpan> Data)> _distributionCache = new();
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(5); // 5分钟缓存过期

    public ReportViewModel()
    {
        _ = LoadPreviewDataAsync();
        _ = LoadChartDataAsync(); // 加载图表数据
        LoadAISettings();
        LoadUserPreferences();
    }

    /// <summary>
    /// 加载 AI 设置
    /// </summary>
    private void LoadAISettings()
    {
        // 从配置管理器加载所有配置
        var configs = _configManager.GetAllConfigs();
        AvailableConfigs = configs.Select(c => c.Name).ToList();

        // 加载默认或上次使用的配置
        var defaultConfig = _configManager.GetDefaultConfig();
        if (defaultConfig != null)
        {
            CurrentConfigName = defaultConfig.Name;
            _originalConfigName = defaultConfig.Name; // 初始化原始名称
            LoadConfigToUI(defaultConfig);
        }

        UpdateAIStatus();
    }

    /// <summary>
    /// 将配置加载到 UI
    /// </summary>
    private void LoadConfigToUI(AIConfig config)
    {
        AiApiKey = config.ApiKey;
        AiModel = config.Model;
        AiBaseUrl = config.BaseUrl;
        AiPrivacyLevel = config.PrivacyLevel;
    }

    /// <summary>
    /// 更新 AI 状态文本
    /// </summary>
    private void UpdateAIStatus()
    {
        IsAIConfigured = !string.IsNullOrWhiteSpace(AiApiKey);
        if (IsAIConfigured)
        {
            AiStatusText = $"{StringResources.Current.AIConfigured} | {CurrentConfigName}";
        }
        else
        {
            AiStatusText = StringResources.Current.AINotConfiguredNeedKey;
        }
    }

    partial void OnAiApiKeyChanged(string value)
    {
        UpdateAIStatus();
    }

    partial void OnCurrentConfigNameChanged(string value)
    {
        // 切换配置时，从配置管理器加载对应配置
        var config = _configManager.GetConfig(value);
        if (config != null)
        {
            LoadConfigToUI(config);
            _configManager.UpdateLastUsed(value);
            _originalConfigName = value; // 更新原始名称
            SaveUserPreferences();
        }
    }

    partial void OnEnableAIAnalysisChanged(bool value)
    {
        SaveUserPreferences();
    }

    partial void OnStartDateChanged(DateTime value)
    {
        StartDateText = value.ToString(StringResources.Current.DateFormatPattern);
        SchedulePreviewLoad();
        ScheduleChartDataLoad();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        EndDateText = value.ToString(StringResources.Current.DateFormatPattern);
        SchedulePreviewLoad();
        ScheduleChartDataLoad();
    }

    /// <summary>
    /// 调度图表数据加载（带节流）
    /// </summary>
    private void ScheduleChartDataLoad()
    {
        // 停止现有的定时器
        _chartLoadDebounceTimer?.Stop();
        _chartLoadDebounceTimer?.Dispose();

        // 创建新的定时器（250ms 节流）
        // 注意：使用 Task.Run 包装避免 async void 导致异常无法捕获
        _chartLoadDebounceTimer = new System.Timers.Timer(250);
        _chartLoadDebounceTimer.Elapsed += (s, e) =>
        {
            _chartLoadDebounceTimer?.Dispose();
            _chartLoadDebounceTimer = null;
            _ = Task.Run(async () =>
            {
                try
                {
                    await LoadChartDataAsync();
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "图表数据加载定时器回调执行失败");
                }
            });
        };
        _chartLoadDebounceTimer.AutoReset = false;
        _chartLoadDebounceTimer.Start();
    }

    private System.Threading.Timer? _previewDebounceTimer;

    private void SchedulePreviewLoad()
    {
        _previewDebounceTimer?.Dispose();
        _previewDebounceTimer = new System.Threading.Timer(
            _ => _ = LoadPreviewDataCoreAsync(),
            null, dueTime: 200, period: Timeout.Infinite);
    }

    private async Task LoadPreviewDataAsync()
    {
        await LoadPreviewDataCoreAsync();
    }

    private async Task LoadPreviewDataCoreAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var start = StartDate;
            var endPlusOne = EndDate.AddDays(1);

            var totalSessions = await dbContext.Sessions
                .CountAsync(s => s.StartTime >= start && s.StartTime < endPlusOne);
            var totalApps = await dbContext.Sessions
                .Where(s => s.StartTime >= start && s.StartTime < endPlusOne)
                .Select(s => s.ProcessName).Distinct().CountAsync();

            TotalDays = (int)(EndDate - StartDate).TotalDays + 1;
            TotalSessions = totalSessions;
            TotalApps = totalApps;

            if (totalSessions == 0)
            {
                StatusDetail = StringResources.Current.NoDataInRange;
            }
            else
            {
                StatusDetail = string.Format(StringResources.Current.RecordsAndApps, TotalSessions, TotalApps);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "加载预览数据失败");
        }
    }

    /// <summary>
    /// 加载图表数据（支持取消、节流和缓存）
    /// </summary>
    private async Task LoadChartDataAsync()
    {
        // 取消之前的加载请求
        _chartLoadCancellationTokenSource?.Cancel();
        _chartLoadCancellationTokenSource?.Dispose();
        _chartLoadCancellationTokenSource = new CancellationTokenSource();

        var cancellationToken = _chartLoadCancellationTokenSource.Token;

        try
        {
            // 生成缓存键
            var cacheKey = $"{StartDate:yyyy-MM-dd}_{EndDate:yyyy-MM-dd}";

            List<DailyUsage> weeklyTrend;
            Dictionary<ActivityType, TimeSpan> activityDistribution;

            // 尝试从缓存获取数据
            var now = DateTime.Now;
            var trendCacheHit = _trendCache.TryGetValue(cacheKey, out var cachedTrend) &&
                                (now - cachedTrend.CacheTime) < _cacheExpiration;
            var distCacheHit = _distributionCache.TryGetValue(cacheKey, out var cachedDist) &&
                               (now - cachedDist.CacheTime) < _cacheExpiration;

            if (trendCacheHit && distCacheHit)
            {
                // 完全命中缓存
                weeklyTrend = cachedTrend.Data;
                activityDistribution = cachedDist.Data;
                Log.Debug("图表数据从缓存加载: {CacheKey}", cacheKey);
            }
            else
            {
                // 需要查询数据库
                await using var dbContext = new RecordTimeDbContext();
                var repository = new SessionRepository(dbContext);

                // 1. 加载趋势数据
                if (trendCacheHit)
                {
                    weeklyTrend = cachedTrend.Data;
                }
                else
                {
                    weeklyTrend = await repository.GetWeeklyTrendAsync(StartDate, EndDate);
                    _trendCache[cacheKey] = (now, weeklyTrend);
                }

                // 检查是否已取消
                if (cancellationToken.IsCancellationRequested)
                {
                    Log.Debug("图表数据加载已取消");
                    return;
                }

                // 2. 加载活动类型分布数据
                if (distCacheHit)
                {
                    activityDistribution = cachedDist.Data;
                }
                else
                {
                    activityDistribution = await repository.GetActivityDistributionAsync(StartDate, EndDate);
                    _distributionCache[cacheKey] = (now, activityDistribution);
                }

                // 再次检查是否已取消
                if (cancellationToken.IsCancellationRequested)
                {
                    Log.Debug("图表数据加载已取消");
                    return;
                }

                Log.Debug("图表数据从数据库加载并缓存: {CacheKey}", cacheKey);
            }

            // 清理过期缓存（保留最近10个）
            CleanupExpiredCache();

            // 在 UI 线程上更新所有图表属性
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                // 配置折线图数据 - 使用 double 类型避免泛型映射问题
                WeeklyTrendSeries = new ISeries[]
                {
                    new LineSeries<double>
                    {
                        Values = weeklyTrend.Select(d => d.TotalHours).ToArray(),
                        Name = StringResources.Current.DailyUsageSeriesName,
                        Fill = null,
                        Stroke = new SolidColorPaint(ChartColorPalette.TextDark) { StrokeThickness = 3 },
                        GeometryFill = new SolidColorPaint(ChartColorPalette.TextDark),
                        GeometryStroke = new SolidColorPaint(ChartColorPalette.TextLight) { StrokeThickness = 2 },
                        GeometrySize = 8,
                        LineSmoothness = 0.65,
                        DataLabelsPaint = new SolidColorPaint(ChartColorPalette.TextMuted),
                        DataLabelsSize = 11,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                        DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F1}h"
                    }
                };

                WeeklyTrendXAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = weeklyTrend.Select(d => d.DateLabel).ToArray(),
                        LabelsRotation = 0,
                        LabelsPaint = new SolidColorPaint(ChartColorPalette.TextMuted),
                        SeparatorsPaint = new SolidColorPaint(ChartColorPalette.GridLine) { StrokeThickness = 1 }
                    }
                };

                WeeklyTrendYAxes = new Axis[]
                {
                    new Axis
                    {
                        Name = StringResources.Current.HoursAxisLabel,
                        NamePaint = new SolidColorPaint(ChartColorPalette.TextMuted),
                        LabelsPaint = new SolidColorPaint(ChartColorPalette.TextMuted),
                        SeparatorsPaint = new SolidColorPaint(ChartColorPalette.GridLine) { StrokeThickness = 1 },
                        Labeler = value => $"{value:F0}h"
                    }
                };

                if (activityDistribution.Count > 0)
                {
                    // 定义活动类型的颜色和名称映射
                    var grays = ChartColorPalette.Grays8;
                    var activityColors = new Dictionary<ActivityType, SKColor>
                    {
                        { ActivityType.Video, grays[0] },
                        { ActivityType.Gaming, grays[1] },
                        { ActivityType.Meeting, grays[2] },
                        { ActivityType.ActiveTyping, grays[3] },
                        { ActivityType.Reading, grays[4] },
                        { ActivityType.PassiveBrowsing, grays[5] },
                        { ActivityType.Idle, ChartColorPalette.Grays5[4] }
                    };

                    var activityNames = new Dictionary<ActivityType, string>
                    {
                        { ActivityType.Video, StringResources.Current.ActivityTypeVideo },
                        { ActivityType.Gaming, StringResources.Current.ActivityTypeGaming },
                        { ActivityType.Meeting, StringResources.Current.ActivityTypeMeeting },
                        { ActivityType.ActiveTyping, StringResources.Current.ActivityTypeActiveTyping },
                        { ActivityType.Reading, StringResources.Current.ActivityTypeReading },
                        { ActivityType.PassiveBrowsing, StringResources.Current.ActivityTypePassiveBrowsing },
                        { ActivityType.Idle, StringResources.Current.ActivityTypeIdle }
                    };

                    ActivityDistributionSeries = activityDistribution
                        .Where(kv => kv.Value.TotalSeconds > 0) // 过滤掉0时长的活动
                        .Select(kv => new PieSeries<double>
                        {
                            Values = new[] { kv.Value.TotalHours },
                            Name = activityNames.GetValueOrDefault(kv.Key, kv.Key.ToString()),
                            Fill = new SolidColorPaint(activityColors.GetValueOrDefault(kv.Key, ChartColorPalette.Fallback)),
                            DataLabelsPaint = new SolidColorPaint(ChartColorPalette.TextLight),
                            DataLabelsSize = 13,
                            DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                            DataLabelsFormatter = point => $"{point.Coordinate.PrimaryValue:F1}h"
                        })
                        .Cast<ISeries>()
                        .ToArray();
                }
                else
                {
                    ActivityDistributionSeries = Array.Empty<ISeries>();
                }

                Log.Debug("图表数据加载完成: {StartDate}至{EndDate} 趋势={TrendCount}天, 活动分布={ActivityCount}类型",
                    StartDate.ToString("yyyy-MM-dd"),
                    EndDate.ToString("yyyy-MM-dd"),
                    weeklyTrend.Count,
                    activityDistribution.Count);
            }, global::Avalonia.Threading.DispatcherPriority.Background);
        }
        catch (OperationCanceledException)
        {
            Log.Debug("图表数据加载被取消");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载图表数据失败");
            // 在 UI 线程上设置空数据避免UI错误
            await global::Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                WeeklyTrendSeries = Array.Empty<ISeries>();
                WeeklyTrendXAxes = Array.Empty<Axis>();
                WeeklyTrendYAxes = Array.Empty<Axis>();
                ActivityDistributionSeries = Array.Empty<ISeries>();
            });
        }
    }

    /// <summary>
    /// 清理过期的缓存条目
    /// </summary>
    private void CleanupExpiredCache()
    {
        var now = DateTime.Now;

        foreach (var key in _trendCache.Keys)
        {
            if (_trendCache.TryGetValue(key, out var val) &&
                (now - val.CacheTime) > _cacheExpiration)
                _trendCache.TryRemove(key, out _);
        }

        foreach (var key in _distributionCache.Keys)
        {
            if (_distributionCache.TryGetValue(key, out var val) &&
                (now - val.CacheTime) > _cacheExpiration)
                _distributionCache.TryRemove(key, out _);
        }

        if (_trendCache.Count > 10)
        {
            foreach (var key in _trendCache.OrderBy(kv => kv.Value.CacheTime)
                .Take(_trendCache.Count - 10).Select(kv => kv.Key).ToList())
                _trendCache.TryRemove(key, out _);
        }

        if (_distributionCache.Count > 10)
        {
            foreach (var key in _distributionCache.OrderBy(kv => kv.Value.CacheTime)
                .Take(_distributionCache.Count - 10).Select(kv => kv.Key).ToList())
                _distributionCache.TryRemove(key, out _);
        }
    }

    public void Dispose()
    {
        _aiCancellationTokenSource?.Cancel();
        _aiCancellationTokenSource?.Dispose();
        _chartLoadCancellationTokenSource?.Cancel();
        _chartLoadCancellationTokenSource?.Dispose();
        _chartLoadDebounceTimer?.Stop();
        _chartLoadDebounceTimer?.Dispose();
        _previewDebounceTimer?.Dispose();
        _trendCache.Clear();
        _distributionCache.Clear();
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        try
        {
            IsGenerating = true;
            StatusText = StringResources.Current.GeneratingReport;
            StatusDetail = StringResources.Current.PleaseWait;

            // 创建报告生成器
            await using var dbContext = new RecordTimeDbContext();
            var reportGenerator = new HtmlReportGenerator(dbContext);

            AIAnalysisResult? aiResult = null;

            // 如果启用了 AI 分析且已配置
            if (EnableAIAnalysis && IsAIConfigured)
            {
                aiResult = await PerformAIAnalysisAsync(dbContext);
            }

            // 生成报告（可能包含 AI 分析结果）
            var reportPath = await reportGenerator.GenerateReportAsync(StartDate, EndDate, aiResult);

            LastReportPath = reportPath;

            if (aiResult != null && aiResult.IsSuccess)
            {
                StatusText = StringResources.Current.ReportSuccessWithAI;
                StatusDetail = $"{StringResources.Current.ProductivityScore}: {aiResult.ProductivityScore} 分 | {StringResources.Current.FileLabel}：{Path.GetFileName(reportPath)}";
            }
            else
            {
                StatusText = StringResources.Current.ReportSuccessWithAI;
                StatusDetail = $"{StringResources.Current.FileNameLabel}：{Path.GetFileName(reportPath)}";
            }
        }
        catch (Exception ex)
        {
            StatusText = StringResources.Current.GenerateFailed;
            StatusDetail = ex.Message;
            Log.Error(ex, "生成报告失败");
        }
        finally
        {
            IsGenerating = false;
        }
    }

    /// <summary>
    /// 执行 AI 分析
    /// </summary>
    private async Task<AIAnalysisResult?> PerformAIAnalysisAsync(RecordTimeDbContext dbContext)
    {
        try
        {
            StatusDetail = StringResources.Current.PerformingAIAnalysis;

            _aiCancellationTokenSource = new CancellationTokenSource();
            _aiCancellationTokenSource.CancelAfter(TimeSpan.FromMinutes(2)); // 2 分钟超时

            // 构建 AI 输入数据
            var dataBuilder = new ReportDataBuilder(dbContext);
            var analysisInput = await dataBuilder.BuildAnalysisInputAsync(StartDate, EndDate, AiPrivacyLevel);

            if (analysisInput.SessionCount == 0)
            {
                Log.Warning("没有数据可供 AI 分析");
                return null;
            }

            // 创建 AI 服务
            var aiService = new OpenAIService(AiApiKey, AiModel, AiBaseUrl);

            if (!aiService.IsAvailable)
            {
                Log.Warning("AI 服务不可用");
                return null;
            }

            // 执行分析
            var result = await aiService.AnalyzeAsync(analysisInput, _aiCancellationTokenSource.Token);

            if (!result.IsSuccess)
            {
                Log.Warning("AI 分析失败: {Error}", result.ErrorMessage);
                StatusDetail = $"AI 分析失败: {result.ErrorMessage}";
            }

            return result;
        }
        catch (OperationCanceledException)
        {
            Log.Warning("AI 分析超时或被取消");
            return null;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "AI 分析出错");
            return null;
        }
        finally
        {
            _aiCancellationTokenSource?.Dispose();
            _aiCancellationTokenSource = null;
        }
    }

    /// <summary>
    /// 测试 AI 连接
    /// </summary>
    [RelayCommand]
    private async Task TestAIConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(AiApiKey))
        {
            AiStatusText = StringResources.Current.PleaseEnterAPIKey;
            ConnectionTestIcon = "⚠️";
            return;
        }

        IsTestingConnection = true;
        AiStatusText = StringResources.Current.TestingConnection;
        ConnectionTestIcon = "🔄";

        try
        {
            var aiService = new OpenAIService(AiApiKey, AiModel, AiBaseUrl);
            var (isValid, errorMessage) = await aiService.ValidateConfigurationAsync();

            if (isValid)
            {
                AiStatusText = StringResources.Current.AIConnectionSuccess;
                ConnectionTestIcon = "✅";
                IsAIConfigured = true;

                // 3秒后恢复默认图标
                await Task.Delay(3000);
                ConnectionTestIcon = "🔗";
            }
            else
            {
                AiStatusText = $"{StringResources.Current.ConnectionFailed}: {errorMessage}";
                ConnectionTestIcon = "❌";
                IsAIConfigured = false;
            }
        }
        catch (Exception ex)
        {
            AiStatusText = $"{StringResources.Current.TestError}: {ex.Message}";
            ConnectionTestIcon = "❌";
            IsAIConfigured = false;
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    /// <summary>
    /// 保存当前 AI 配置（支持重命名）
    /// </summary>
    [RelayCommand]
    private void SaveAIConfig()
    {
        try
        {
            // 检查是否重命名了配置
            if (_originalConfigName != CurrentConfigName)
            {
                // 执行重命名
                if (_configManager.RenameConfig(_originalConfigName, CurrentConfigName))
                {
                    _originalConfigName = CurrentConfigName;
                    Log.Information("配置已重命名为: {NewName}", CurrentConfigName);
                }
                else
                {
                    AiStatusText = string.Format(StringResources.Current.ConfigNameExists, CurrentConfigName);
                    CurrentConfigName = _originalConfigName; // 恢复原名称
                    return;
                }
            }

            var config = new AIConfig
            {
                Name = CurrentConfigName,
                ApiKey = AiApiKey,
                Model = AiModel,
                BaseUrl = AiBaseUrl,
                PrivacyLevel = AiPrivacyLevel,
                IsDefault = _configManager.GetDefaultConfig()?.Name == CurrentConfigName
            };

            _configManager.SaveConfig(config);

            // 刷新配置列表
            var configs = _configManager.GetAllConfigs();
            AvailableConfigs = configs.Select(c => c.Name).ToList();

            AiStatusText = $"{StringResources.Current.ConfigSaved} | {CurrentConfigName}";
            Log.Information("AI 配置已保存: {Name}", CurrentConfigName);
        }
        catch (Exception ex)
        {
            AiStatusText = $"{StringResources.Current.SaveFailed}: {ex.Message}";
            Log.Error(ex, "保存 AI 配置失败");
        }
    }

    /// <summary>
    /// 删除当前 AI 配置
    /// </summary>
    [RelayCommand]
    private void DeleteAIConfig()
    {
        try
        {
            if (_configManager.GetAllConfigs().Count <= 1)
            {
                AiStatusText = StringResources.Current.CannotDeleteLastConfig;
                return;
            }

            if (_configManager.DeleteConfig(CurrentConfigName))
            {
                // 加载第一个可用配置
                var firstConfig = _configManager.GetAllConfigs().FirstOrDefault();
                if (firstConfig != null)
                {
                    CurrentConfigName = firstConfig.Name;
                    LoadConfigToUI(firstConfig);
                }

                // 刷新配置列表
                var configs = _configManager.GetAllConfigs();
                AvailableConfigs = configs.Select(c => c.Name).ToList();

                AiStatusText = StringResources.Current.ConfigDeleted;
                Log.Information("AI 配置已删除");
            }
        }
        catch (Exception ex)
        {
            AiStatusText = $"{StringResources.Current.DeleteFailed}: {ex.Message}";
            Log.Error(ex, "删除 AI 配置失败");
        }
    }

    /// <summary>
    /// 添加新的 AI 配置
    /// </summary>
    [RelayCommand]
    private void AddNewAIConfig()
    {
        try
        {
            // 生成新配置名称
            int count = 1;
            string newName;
            do
            {
                newName = string.Format(StringResources.Current.NewConfigPattern, count);
                count++;
            } while (_configManager.ConfigExists(newName));

            var newConfig = new AIConfig
            {
                Name = newName,
                Model = "gpt-4o-mini",
                BaseUrl = "https://api.openai.com/v1"
            };

            _configManager.SaveConfig(newConfig);

            // 刷新配置列表并切换到新配置
            var configs = _configManager.GetAllConfigs();
            AvailableConfigs = configs.Select(c => c.Name).ToList();
            CurrentConfigName = newName;

            AiStatusText = $"{StringResources.Current.NewConfigCreated} | {newName}";
            Log.Information("创建新 AI 配置: {Name}", newName);
        }
        catch (Exception ex)
        {
            AiStatusText = $"{StringResources.Current.CreateFailed}: {ex.Message}";
            Log.Error(ex, "创建新 AI 配置失败");
        }
    }

    /// <summary>
    /// 重命名当前 AI 配置
    /// </summary>
    [RelayCommand]
    private void RenameAIConfig()
    {
        try
        {
            // TODO: 这里需要一个输入对话框，暂时先记录功能
            // 实际实现时应该弹出一个对话框让用户输入新名称
            AiStatusText = StringResources.Current.RenameNeedsDialog;
            Log.Information("重命名配置功能被调用");
        }
        catch (Exception ex)
        {
            AiStatusText = $"{StringResources.Current.RenameFailed}: {ex.Message}";
            Log.Error(ex, "重命名 AI 配置失败");
        }
    }

    [RelayCommand]
    private void OpenReport()
    {
        if (string.IsNullOrEmpty(LastReportPath) || !File.Exists(LastReportPath))
        {
            StatusText = StringResources.Current.ReportFileNotExists;
            return;
        }

        try
        {
            // 在默认浏览器中打开 HTML 报告
            Process.Start(new ProcessStartInfo
            {
                FileName = LastReportPath,
                UseShellExecute = true
            });
            StatusText = StringResources.Current.ReportOpenedInBrowser;
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.OpenFailed}：{ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenReportsFolder()
    {
        try
        {
            var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");

            if (!Directory.Exists(reportsFolder))
            {
                StatusText = StringResources.Current.ReportFolderNotExists;
                return;
            }

            // 在文件资源管理器中打开文件夹
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = reportsFolder,
                UseShellExecute = true
            });
            StatusText = StringResources.Current.ReportFolderOpened;
        }
        catch (Exception ex)
        {
            StatusText = $"{StringResources.Current.OpenFailed}：{ex.Message}";
        }
    }

    [RelayCommand]
    private void SetStartDateToday()
    {
        StartDate = DateTime.Today;
        StartDateText = StartDate.ToString(StringResources.Current.DateFormatPattern);
    }

    [RelayCommand]
    private void SetEndDateToday()
    {
        EndDate = DateTime.Today;
        EndDateText = EndDate.ToString(StringResources.Current.DateFormatPattern);
    }

    [RelayCommand]
    private void SetLastWeek()
    {
        StartDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        StartDateText = StartDate.ToString(StringResources.Current.DateFormatPattern);
        EndDateText = EndDate.ToString(StringResources.Current.DateFormatPattern);
        IsCustomDateMode = false;
    }

    [RelayCommand]
    private void SetLastMonth()
    {
        StartDate = DateTime.Today.AddMonths(-1);
        EndDate = DateTime.Today;
        IsCustomDateMode = false;
    }

    [RelayCommand]
    private void SetLastThreeMonths()
    {
        StartDate = DateTime.Today.AddMonths(-3);
        EndDate = DateTime.Today;
        IsCustomDateMode = false;
    }

    [RelayCommand]
    private void ToggleCustomDate()
    {
        IsCustomDateMode = !IsCustomDateMode;
    }

    /// <summary>
    /// 加载用户偏好设置
    /// </summary>
    private void LoadUserPreferences()
    {
        try
        {
            var prefsPath = GetUserPreferencesPath();
            if (!File.Exists(prefsPath))
            {
                return;
            }

            var json = File.ReadAllText(prefsPath);
            var prefs = System.Text.Json.JsonSerializer.Deserialize<UserPreferences>(json);

            if (prefs != null)
            {
                EnableAIAnalysis = prefs.EnableAIAnalysis;

                // 恢复上次选择的配置
                if (!string.IsNullOrEmpty(prefs.LastSelectedConfig) &&
                    _configManager.ConfigExists(prefs.LastSelectedConfig))
                {
                    CurrentConfigName = prefs.LastSelectedConfig;
                    _originalConfigName = prefs.LastSelectedConfig;
                }

                Log.Information("用户偏好已加载");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "加载用户偏好失败");
        }
    }

    /// <summary>
    /// 保存用户偏好设置
    /// </summary>
    private void SaveUserPreferences()
    {
        try
        {
            var prefs = new UserPreferences
            {
                EnableAIAnalysis = EnableAIAnalysis,
                LastSelectedConfig = CurrentConfigName
            };

            var options = new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = System.Text.Json.JsonSerializer.Serialize(prefs, options);
            var prefsPath = GetUserPreferencesPath();

            File.WriteAllText(prefsPath, json);
            Log.Debug("用户偏好已保存");
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "保存用户偏好失败");
        }
    }

    /// <summary>
    /// 获取用户偏好文件路径
    /// </summary>
    private static string GetUserPreferencesPath()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime"
        );
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "report_preferences.json");
    }

    /// <summary>
    /// 用户偏好数据类
    /// </summary>
    private class UserPreferences
    {
        public bool EnableAIAnalysis { get; set; }
        public string LastSelectedConfig { get; set; } = string.Empty;
    }
}
