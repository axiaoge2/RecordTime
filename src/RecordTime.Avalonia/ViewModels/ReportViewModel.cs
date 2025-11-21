using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Data;
using RecordTime.Data.Reports;
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
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

public partial class ReportViewModel : ViewModelBase
{
    [ObservableProperty]
    private DateTime _startDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime _endDate = DateTime.Today;

    [ObservableProperty]
    private string _startDateText = DateTime.Today.AddDays(-7).ToString("yyyy年MM月dd日");

    [ObservableProperty]
    private string _endDateText = DateTime.Today.ToString("yyyy年MM月dd日");

    [ObservableProperty]
    private string _statusText = "准备生成报告";

    [ObservableProperty]
    private string? _statusDetail;

    [ObservableProperty]
    private bool _isGenerating = false;

    [ObservableProperty]
    private string? _lastReportPath;

    [ObservableProperty]
    private int _totalDays = 0;

    [ObservableProperty]
    private int _totalSessions = 0;

    [ObservableProperty]
    private int _totalApps = 0;

    // AI 相关属性
    [ObservableProperty]
    private bool _enableAIAnalysis = false;

    [ObservableProperty]
    private bool _isAIConfigured = false;

    [ObservableProperty]
    private string _aiStatusText = "AI 分析未配置";

    [ObservableProperty]
    private string _aiApiKey = string.Empty;

    [ObservableProperty]
    private string _aiModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _aiBaseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private AIPrivacyLevel _aiPrivacyLevel = AIPrivacyLevel.CategoryOnly;

    [ObservableProperty]
    private string _currentConfigName = "OpenAI 官方";

    [ObservableProperty]
    private List<string> _availableConfigs = new();

    private string _originalConfigName = "OpenAI 官方"; // 用于跟踪配置重命名

    [ObservableProperty]
    private bool _isTestingConnection = false;

    [ObservableProperty]
    private string _connectionTestIcon = "🔗";

    private readonly AIConfigManager _configManager = new();
    private CancellationTokenSource? _aiCancellationTokenSource;

    public ReportViewModel()
    {
        _ = LoadPreviewDataAsync();
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
            AiStatusText = $"AI 已配置 | {CurrentConfigName}";
        }
        else
        {
            AiStatusText = "AI 分析未配置（需要 API Key）";
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
        StartDateText = value.ToString("yyyy年MM月dd日");
        _ = LoadPreviewDataAsync();
    }

    partial void OnEndDateChanged(DateTime value)
    {
        EndDateText = value.ToString("yyyy年MM月dd日");
        _ = LoadPreviewDataAsync();
    }

    private async Task LoadPreviewDataAsync()
    {
        try
        {
            await using var dbContext = new RecordTimeDbContext();

            var sessions = await dbContext.Sessions
                .Where(s => s.StartTime >= StartDate && s.StartTime < EndDate.AddDays(1))
                .ToListAsync();

            TotalDays = (int)(EndDate - StartDate).TotalDays + 1;
            TotalSessions = sessions.Count;
            TotalApps = sessions.Select(s => s.ProcessName).Distinct().Count();

            if (sessions.Count == 0)
            {
                StatusDetail = "所选时间范围内没有数据";
            }
            else
            {
                StatusDetail = $"共有 {TotalSessions} 条记录，涉及 {TotalApps} 个应用";
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"加载预览数据失败: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task GenerateReportAsync()
    {
        try
        {
            IsGenerating = true;
            StatusText = "正在生成报告...";
            StatusDetail = "请稍候，这可能需要几秒钟";

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
                StatusText = "✓ 报告生成成功（含 AI 分析）";
                StatusDetail = $"效率评分: {aiResult.ProductivityScore} 分 | 文件：{Path.GetFileName(reportPath)}";
            }
            else
            {
                StatusText = "✓ 报告生成成功";
                StatusDetail = $"文件名：{Path.GetFileName(reportPath)}";
            }
        }
        catch (Exception ex)
        {
            StatusText = "✗ 生成失败";
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
            StatusDetail = "正在进行 AI 分析...";

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
            AiStatusText = "⚠️ 请先输入 API Key";
            ConnectionTestIcon = "⚠️";
            return;
        }

        IsTestingConnection = true;
        AiStatusText = "🔄 正在测试连接...";
        ConnectionTestIcon = "🔄";

        try
        {
            var aiService = new OpenAIService(AiApiKey, AiModel, AiBaseUrl);
            var (isValid, errorMessage) = await aiService.ValidateConfigurationAsync();

            if (isValid)
            {
                AiStatusText = "✅ AI 连接成功！配置已验证";
                ConnectionTestIcon = "✅";
                IsAIConfigured = true;

                // 3秒后恢复默认图标
                await Task.Delay(3000);
                ConnectionTestIcon = "🔗";
            }
            else
            {
                AiStatusText = $"❌ 连接失败: {errorMessage}";
                ConnectionTestIcon = "❌";
                IsAIConfigured = false;
            }
        }
        catch (Exception ex)
        {
            AiStatusText = $"❌ 测试出错: {ex.Message}";
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
                    AiStatusText = $"❌ 配置名称 '{CurrentConfigName}' 已存在或无效";
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

            AiStatusText = $"✅ 配置已保存 | {CurrentConfigName}";
            Log.Information("AI 配置已保存: {Name}", CurrentConfigName);
        }
        catch (Exception ex)
        {
            AiStatusText = $"❌ 保存失败: {ex.Message}";
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
                AiStatusText = "✗ 无法删除最后一个配置";
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

                AiStatusText = "✓ 配置已删除";
                Log.Information("AI 配置已删除");
            }
        }
        catch (Exception ex)
        {
            AiStatusText = $"✗ 删除失败: {ex.Message}";
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
                newName = $"配置 {count}";
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

            AiStatusText = $"✓ 新配置已创建 | {newName}";
            Log.Information("创建新 AI 配置: {Name}", newName);
        }
        catch (Exception ex)
        {
            AiStatusText = $"✗ 创建失败: {ex.Message}";
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
            AiStatusText = "重命名功能需要对话框支持";
            Log.Information("重命名配置功能被调用");
        }
        catch (Exception ex)
        {
            AiStatusText = $"✗ 重命名失败: {ex.Message}";
            Log.Error(ex, "重命名 AI 配置失败");
        }
    }

    [RelayCommand]
    private void OpenReport()
    {
        if (string.IsNullOrEmpty(LastReportPath) || !File.Exists(LastReportPath))
        {
            StatusText = "报告文件不存在";
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
            StatusText = "已在浏览器中打开报告";
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败：{ex.Message}";
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
                StatusText = "报告文件夹不存在";
                return;
            }

            // 在文件资源管理器中打开文件夹
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = reportsFolder,
                UseShellExecute = true
            });
            StatusText = "已打开报告文件夹";
        }
        catch (Exception ex)
        {
            StatusText = $"打开失败：{ex.Message}";
        }
    }

    [RelayCommand]
    private void SetStartDateToday()
    {
        StartDate = DateTime.Today;
        StartDateText = StartDate.ToString("yyyy年MM月dd日");
    }

    [RelayCommand]
    private void SetEndDateToday()
    {
        EndDate = DateTime.Today;
        EndDateText = EndDate.ToString("yyyy年MM月dd日");
    }

    [RelayCommand]
    private void SetLastWeek()
    {
        StartDate = DateTime.Today.AddDays(-7);
        EndDate = DateTime.Today;
        StartDateText = StartDate.ToString("yyyy年MM月dd日");
        EndDateText = EndDate.ToString("yyyy年MM月dd日");
    }

    [RelayCommand]
    private void SetLastMonth()
    {
        StartDate = DateTime.Today.AddMonths(-1);
        EndDate = DateTime.Today;
    }

    [RelayCommand]
    private void SetLastThreeMonths()
    {
        StartDate = DateTime.Today.AddMonths(-3);
        EndDate = DateTime.Today;
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
