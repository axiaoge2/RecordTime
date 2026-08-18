using RecordTime.Core.Models;
using Serilog;
using System.Text.Json;

namespace RecordTime.Core.Services;

/// <summary>
/// 应用设置管理服务
/// 负责加载、保存和管理应用程序配置
/// </summary>
public class AppSettingsService
{
    private readonly string _settingsFilePath;
    private AppSettings? _currentSettings;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AppSettingsService(string? settingsPath = null)
    {
        // 默认保存在用户本地数据目录
        var appDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime"
        );

        Directory.CreateDirectory(appDataFolder);

        _settingsFilePath = settingsPath ?? Path.Combine(appDataFolder, "appsettings.json");

        Log.Debug("AppSettingsService 初始化，配置文件路径: {Path}", _settingsFilePath);
    }

    /// <summary>
    /// 获取当前设置（单例模式）
    /// </summary>
    public AppSettings GetSettings()
    {
        lock (_lock)
        {
            if (_currentSettings == null)
            {
                _currentSettings = LoadSettings();
            }
            return _currentSettings;
        }
    }

    /// <summary>
    /// 获取会话空闲超时时间（秒）
    /// </summary>
    public int GetIdleTimeoutSeconds()
    {
        return GetSettings().Monitoring.IdleTimeoutMinutes * 60;
    }

    /// <summary>
    /// 加载设置
    /// </summary>
    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

                if (settings != null)
                {
                    Log.Information("成功加载设置文件: {Path}", _settingsFilePath);
                    return settings;
                }
            }

            Log.Information("设置文件不存在，使用默认设置: {Path}", _settingsFilePath);
            return new AppSettings();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载设置文件失败，使用默认设置");
            return new AppSettings();
        }
    }

    /// <summary>
    /// 保存设置
    /// </summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        try
        {
            lock (_lock)
            {
                _currentSettings = settings;
            }

            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(_settingsFilePath, json);

            Log.Information("设置已保存: {Path}", _settingsFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存设置失败");
            throw;
        }
    }

    /// <summary>
    /// 更新设置（部分更新）
    /// </summary>
    public async Task UpdateSettingsAsync(Action<AppSettings> updateAction)
    {
        var settings = GetSettings();
        updateAction(settings);
        await SaveSettingsAsync(settings);
    }

    /// <summary>
    /// 重置为默认设置
    /// </summary>
    public async Task ResetToDefaultAsync()
    {
        var defaultSettings = new AppSettings();
        await SaveSettingsAsync(defaultSettings);
        Log.Information("设置已重置为默认值");
    }

    /// <summary>
    /// 导出设置到指定路径
    /// </summary>
    public async Task ExportSettingsAsync(string exportPath)
    {
        try
        {
            var settings = GetSettings();
            var json = JsonSerializer.Serialize(settings, JsonOptions);
            await File.WriteAllTextAsync(exportPath, json);
            Log.Information("设置已导出到: {Path}", exportPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导出设置失败");
            throw;
        }
    }

    /// <summary>
    /// 从文件导入设置
    /// </summary>
    public async Task ImportSettingsAsync(string importPath)
    {
        try
        {
            if (!File.Exists(importPath))
            {
                throw new FileNotFoundException("导入文件不存在", importPath);
            }

            var json = await File.ReadAllTextAsync(importPath);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions);

            if (settings == null)
            {
                throw new InvalidDataException("导入文件格式无效");
            }

            await SaveSettingsAsync(settings);
            Log.Information("设置已从文件导入: {Path}", importPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "导入设置失败");
            throw;
        }
    }

    /// <summary>
    /// 获取设置文件路径
    /// </summary>
    public string GetSettingsFilePath() => _settingsFilePath;
}
