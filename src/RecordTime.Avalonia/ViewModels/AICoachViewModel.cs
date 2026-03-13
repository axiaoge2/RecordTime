using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Data.Converters;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RecordTime.Avalonia.Services;
using RecordTime.Core.Models.AI;
using RecordTime.Core.Models.AICoach;
using RecordTime.Core.Services.AI;
using RecordTime.Core.Services.AICoach;
using Serilog;

namespace RecordTime.Avalonia.ViewModels;

/// <summary>
/// AI Coach 视图模型
/// </summary>
public partial class AICoachViewModel : ViewModelBase
{
    /// <summary>
    /// 用户消息角色转换器
    /// </summary>
    public static readonly IValueConverter IsUserRole =
        new FuncValueConverter<MessageRole, bool>(role => role == MessageRole.User);

    /// <summary>
    /// 助手消息角色转换器
    /// </summary>
    public static readonly IValueConverter IsAssistantRole =
        new FuncValueConverter<MessageRole, bool>(role => role == MessageRole.Assistant);

    private IAICoachService? _aiService;
    private readonly CognitiveContextBuilder? _contextBuilder;
    private readonly PromptBuilder? _promptBuilder;
    private readonly AIConfigManager _configManager = new();

    [ObservableProperty]
    private bool _isPanelOpen;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _userInput = string.Empty;

    [ObservableProperty]
    private string _currentResponse = string.Empty;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isConfigured;

    // ========== 设置相关属性 ==========

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private string _settingsApiKey = string.Empty;

    [ObservableProperty]
    private string _settingsModel = "gpt-4o-mini";

    [ObservableProperty]
    private string _settingsBaseUrl = "https://api.openai.com/v1";

    [ObservableProperty]
    private string _currentConfigName = "默认配置";

    [ObservableProperty]
    private List<string> _availableConfigs = new();

    [ObservableProperty]
    private bool _isTestingConnection;

    [ObservableProperty]
    private string _connectionStatus = string.Empty;

    [ObservableProperty]
    private string _connectionStatusIcon = "🔗";

    /// <summary>
    /// 对话历史
    /// </summary>
    public ObservableCollection<AICoachMessage> Messages { get; } = new();

    public AICoachViewModel()
    {
        // 设计时使用
        LoadAISettings();
    }

    public AICoachViewModel(CognitiveContextBuilder contextBuilder, PromptBuilder promptBuilder)
    {
        _contextBuilder = contextBuilder;
        _promptBuilder = promptBuilder;
        LoadAISettings();
        RecreateAIService();
    }

    // 保留旧的构造函数以兼容
    public AICoachViewModel(IAICoachService aiService, CognitiveContextBuilder contextBuilder)
    {
        _aiService = aiService;
        _contextBuilder = contextBuilder;
        LoadAISettings();
    }

    /// <summary>
    /// 加载 AI 设置
    /// </summary>
    private void LoadAISettings()
    {
        try
        {
            var configs = _configManager.GetAllConfigs();
            AvailableConfigs = configs.Select(c => c.Name).ToList();

            var defaultConfig = _configManager.GetDefaultConfig();
            if (defaultConfig != null)
            {
                CurrentConfigName = defaultConfig.Name;
                LoadConfigToSettings(defaultConfig);
            }

            UpdateConfigurationStatus();
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "加载 AI 设置失败");
        }
    }

    /// <summary>
    /// 将配置加载到设置
    /// </summary>
    private void LoadConfigToSettings(AIConfig config)
    {
        SettingsApiKey = config.ApiKey;
        SettingsModel = config.Model;
        SettingsBaseUrl = config.BaseUrl;
    }

    /// <summary>
    /// 更新配置状态
    /// </summary>
    private void UpdateConfigurationStatus()
    {
        IsConfigured = !string.IsNullOrWhiteSpace(SettingsApiKey);
        if (IsConfigured)
        {
            ConnectionStatus = $"已配置: {CurrentConfigName}";
            ConnectionStatusIcon = "✅";
        }
        else
        {
            ConnectionStatus = "未配置 API Key";
            ConnectionStatusIcon = "⚠️";
        }
    }

    /// <summary>
    /// 重新创建 AI 服务
    /// </summary>
    private void RecreateAIService()
    {
        if (_promptBuilder == null) return;

        try
        {
            var settings = new AICoachSettings
            {
                ApiBaseUrl = SettingsBaseUrl,
                ApiKey = SettingsApiKey,
                Model = SettingsModel,
                MaxTokens = 1000,
                Temperature = 0.7,
                TimeoutSeconds = 60
            };

            // 释放旧的服务
            if (_aiService is IDisposable disposable)
            {
                disposable.Dispose();
            }

            _aiService = new AICoachService(_promptBuilder, settings);
            UpdateConfigurationStatus();
            Log.Information("AI Coach 服务已重新创建: Model={Model}, BaseUrl={BaseUrl}", settings.Model, settings.ApiBaseUrl);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "创建 AI Coach 服务失败");
        }
    }

    partial void OnCurrentConfigNameChanged(string value)
    {
        var config = _configManager.GetConfig(value);
        if (config != null)
        {
            LoadConfigToSettings(config);
            _configManager.UpdateLastUsed(value);
            RecreateAIService();
        }
    }

    /// <summary>
    /// 初始化（检查服务可用性）
    /// </summary>
    public async Task InitializeAsync()
    {
        if (_aiService == null)
        {
            IsConfigured = false;
            ErrorMessage = "AI 服务未配置，请点击设置按钮配置 API";
            HasError = !string.IsNullOrWhiteSpace(SettingsApiKey); // 只有配置了但服务创建失败才显示错误
            return;
        }

        try
        {
            var isAvailable = await _aiService.IsAvailableAsync();
            if (!isAvailable && !string.IsNullOrWhiteSpace(SettingsApiKey))
            {
                // 配置了但不可用
                ErrorMessage = "API 连接失败，请检查配置";
                HasError = true;
            }
            else if (!isAvailable)
            {
                // 未配置
                IsConfigured = false;
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "检查 AI 服务可用性失败");
            if (!string.IsNullOrWhiteSpace(SettingsApiKey))
            {
                ErrorMessage = "AI 服务不可用";
                HasError = true;
            }
        }
    }

    /// <summary>
    /// 切换面板显示
    /// </summary>
    [RelayCommand]
    private void TogglePanel()
    {
        IsPanelOpen = !IsPanelOpen;
        HasError = false;
    }

    /// <summary>
    /// 关闭面板
    /// </summary>
    [RelayCommand]
    private void ClosePanel()
    {
        IsPanelOpen = false;
        IsSettingsOpen = false;
    }

    // ========== 设置相关命令 ==========

    /// <summary>
    /// 打开设置面板
    /// </summary>
    [RelayCommand]
    private void OpenSettings()
    {
        IsSettingsOpen = true;
        HasError = false;
    }

    /// <summary>
    /// 关闭设置面板
    /// </summary>
    [RelayCommand]
    private void CloseSettings()
    {
        IsSettingsOpen = false;
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    [RelayCommand]
    private void SaveSettings()
    {
        try
        {
            var config = new AIConfig
            {
                Name = CurrentConfigName,
                ApiKey = SettingsApiKey,
                Model = SettingsModel,
                BaseUrl = SettingsBaseUrl,
                IsDefault = _configManager.GetDefaultConfig()?.Name == CurrentConfigName
            };

            _configManager.SaveConfig(config);

            // 刷新配置列表
            var configs = _configManager.GetAllConfigs();
            AvailableConfigs = configs.Select(c => c.Name).ToList();

            // 重新创建服务
            RecreateAIService();

            ConnectionStatus = "设置已保存";
            ConnectionStatusIcon = "✅";
            Log.Information("AI Coach 设置已保存: {ConfigName}", CurrentConfigName);

            // 关闭设置面板
            IsSettingsOpen = false;
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"保存失败: {ex.Message}";
            ConnectionStatusIcon = "❌";
            Log.Error(ex, "保存 AI Coach 设置失败");
        }
    }

    /// <summary>
    /// 测试连接
    /// </summary>
    [RelayCommand]
    private async Task TestConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(SettingsApiKey))
        {
            ConnectionStatus = "请输入 API Key";
            ConnectionStatusIcon = "⚠️";
            return;
        }

        IsTestingConnection = true;
        ConnectionStatus = "测试连接中...";
        ConnectionStatusIcon = "🔄";

        try
        {
            var settings = new AICoachSettings
            {
                ApiBaseUrl = SettingsBaseUrl,
                ApiKey = SettingsApiKey,
                Model = SettingsModel,
                MaxTokens = 5,
                TimeoutSeconds = 30
            };

            using var testService = new AICoachService(_promptBuilder ?? new PromptBuilder(new KnowledgeBaseProvider()), settings);
            var isAvailable = await testService.IsAvailableAsync();

            if (isAvailable)
            {
                ConnectionStatus = "连接成功 ✓";
                ConnectionStatusIcon = "✅";
                IsConfigured = true;
            }
            else
            {
                ConnectionStatus = "连接失败，请检查 API Key";
                ConnectionStatusIcon = "❌";
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"连接错误: {ex.Message}";
            ConnectionStatusIcon = "❌";
            Log.Error(ex, "测试 AI 连接失败");
        }
        finally
        {
            IsTestingConnection = false;
        }
    }

    /// <summary>
    /// 添加新配置
    /// </summary>
    [RelayCommand]
    private void AddNewConfig()
    {
        try
        {
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

            var configs = _configManager.GetAllConfigs();
            AvailableConfigs = configs.Select(c => c.Name).ToList();
            CurrentConfigName = newName;

            ConnectionStatus = $"已创建: {newName}";
            Log.Information("创建新 AI 配置: {Name}", newName);
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"创建失败: {ex.Message}";
            Log.Error(ex, "创建新 AI 配置失败");
        }
    }

    /// <summary>
    /// 删除当前配置
    /// </summary>
    [RelayCommand]
    private void DeleteConfig()
    {
        try
        {
            if (_configManager.GetAllConfigs().Count <= 1)
            {
                ConnectionStatus = "无法删除最后一个配置";
                return;
            }

            if (_configManager.DeleteConfig(CurrentConfigName))
            {
                var firstConfig = _configManager.GetAllConfigs().FirstOrDefault();
                if (firstConfig != null)
                {
                    CurrentConfigName = firstConfig.Name;
                    LoadConfigToSettings(firstConfig);
                }

                var configs = _configManager.GetAllConfigs();
                AvailableConfigs = configs.Select(c => c.Name).ToList();

                ConnectionStatus = "配置已删除";
                RecreateAIService();
                Log.Information("AI 配置已删除");
            }
        }
        catch (Exception ex)
        {
            ConnectionStatus = $"删除失败: {ex.Message}";
            Log.Error(ex, "删除 AI 配置失败");
        }
    }

    /// <summary>
    /// 今日总结
    /// </summary>
    [RelayCommand]
    private async Task DailySummaryAsync()
    {
        await SendQuickActionAsync(QuickActionType.DailySummary, "📊 今日总结");
    }

    /// <summary>
    /// 即时建议
    /// </summary>
    [RelayCommand]
    private async Task QuickAdviceAsync()
    {
        await SendQuickActionAsync(QuickActionType.QuickAdvice, "💡 给我建议");
    }

    /// <summary>
    /// 明日规划
    /// </summary>
    [RelayCommand]
    private async Task TomorrowPlanAsync()
    {
        await SendQuickActionAsync(QuickActionType.TomorrowPlan, "🎯 规划明天");
    }

    /// <summary>
    /// 发送用户输入
    /// </summary>
    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(UserInput))
            return;

        var message = UserInput.Trim();
        UserInput = string.Empty;

        await SendChatAsync(message);
    }

    /// <summary>
    /// 发送快捷操作
    /// </summary>
    private async Task SendQuickActionAsync(QuickActionType actionType, string displayName)
    {
        if (_aiService == null || _contextBuilder == null)
        {
            ShowError("AI 服务未配置");
            return;
        }

        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            HasError = false;
            CurrentResponse = string.Empty;

            // 添加用户消息
            Messages.Add(new AICoachMessage
            {
                Role = MessageRole.User,
                Content = displayName
            });

            // 构建上下文
            var context = await _contextBuilder.BuildContextAsync();

            // 发送请求
            var response = await _aiService.SendQuickActionAsync(context, actionType);

            if (response.Success)
            {
                CurrentResponse = response.Content;
                Messages.Add(new AICoachMessage
                {
                    Role = MessageRole.Assistant,
                    Content = response.Content
                });
            }
            else
            {
                ShowError(response.ErrorMessage ?? "请求失败");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送快捷操作失败: {ActionType}", actionType);
            ShowError($"请求失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 发送自由对话
    /// </summary>
    private async Task SendChatAsync(string message)
    {
        if (_aiService == null || _contextBuilder == null)
        {
            ShowError("AI 服务未配置");
            return;
        }

        if (IsLoading)
            return;

        try
        {
            IsLoading = true;
            HasError = false;
            CurrentResponse = string.Empty;

            // 添加用户消息
            Messages.Add(new AICoachMessage
            {
                Role = MessageRole.User,
                Content = message
            });

            // 构建上下文
            var context = await _contextBuilder.BuildContextAsync();

            // 发送请求
            var response = await _aiService.SendChatAsync(context, message);

            if (response.Success)
            {
                CurrentResponse = response.Content;
                Messages.Add(new AICoachMessage
                {
                    Role = MessageRole.Assistant,
                    Content = response.Content
                });
            }
            else
            {
                ShowError(response.ErrorMessage ?? "请求失败");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送对话失败");
            ShowError($"请求失败: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// 显示错误
    /// </summary>
    private void ShowError(string message)
    {
        ErrorMessage = message;
        HasError = true;
    }

    /// <summary>
    /// 清空对话
    /// </summary>
    [RelayCommand]
    private void ClearMessages()
    {
        Messages.Clear();
        CurrentResponse = string.Empty;
        HasError = false;
    }
}
