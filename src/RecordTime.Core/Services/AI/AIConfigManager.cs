using System.Text.Json;
using RecordTime.Core.Models.AI;
using Serilog;

namespace RecordTime.Core.Services.AI;

/// <summary>
/// AI 配置管理器 - 负责配置的保存、加载和管理
/// </summary>
public class AIConfigManager
{
    private readonly string _configFilePath;
    private List<AIConfig> _configs = new();

    public AIConfigManager()
    {
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime"
        );
        Directory.CreateDirectory(appDataPath);
        _configFilePath = Path.Combine(appDataPath, "ai_configs.json");

        LoadConfigs();
    }

    /// <summary>
    /// 获取所有配置
    /// </summary>
    public List<AIConfig> GetAllConfigs()
    {
        return _configs.OrderByDescending(c => c.LastUsedAt).ToList();
    }

    /// <summary>
    /// 获取默认配置
    /// </summary>
    public AIConfig? GetDefaultConfig()
    {
        return _configs.FirstOrDefault(c => c.IsDefault) ?? _configs.FirstOrDefault();
    }

    /// <summary>
    /// 保存或更新配置
    /// </summary>
    public void SaveConfig(AIConfig config)
    {
        // 查找是否已存在同名配置
        var existing = _configs.FirstOrDefault(c => c.Name == config.Name);
        if (existing != null)
        {
            // 更新现有配置
            existing.ApiKey = config.ApiKey;
            existing.Model = config.Model;
            existing.BaseUrl = config.BaseUrl;
            existing.PrivacyLevel = config.PrivacyLevel;
            existing.LastUsedAt = DateTime.Now;
        }
        else
        {
            // 添加新配置
            config.CreatedAt = DateTime.Now;
            config.LastUsedAt = DateTime.Now;
            _configs.Add(config);
        }

        // 如果设置为默认，取消其他配置的默认状态
        if (config.IsDefault)
        {
            foreach (var c in _configs.Where(c => c.Name != config.Name))
            {
                c.IsDefault = false;
            }
        }

        SaveToFile();
    }

    /// <summary>
    /// 删除配置
    /// </summary>
    public bool DeleteConfig(string name)
    {
        var config = _configs.FirstOrDefault(c => c.Name == name);
        if (config == null) return false;

        // 不允许删除最后一个配置
        if (_configs.Count == 1)
        {
            Log.Warning("无法删除最后一个 AI 配置");
            return false;
        }

        _configs.Remove(config);

        // 如果删除的是默认配置，将第一个配置设为默认
        if (config.IsDefault && _configs.Count > 0)
        {
            _configs[0].IsDefault = true;
        }

        SaveToFile();
        return true;
    }

    /// <summary>
    /// 设置默认配置
    /// </summary>
    public void SetDefaultConfig(string name)
    {
        foreach (var config in _configs)
        {
            config.IsDefault = (config.Name == name);
        }
        SaveToFile();
    }

    /// <summary>
    /// 更新最后使用时间
    /// </summary>
    public void UpdateLastUsed(string name)
    {
        var config = _configs.FirstOrDefault(c => c.Name == name);
        if (config != null)
        {
            config.LastUsedAt = DateTime.Now;
            SaveToFile();
        }
    }

    /// <summary>
    /// 获取指定配置
    /// </summary>
    public AIConfig? GetConfig(string name)
    {
        return _configs.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>
    /// 配置名称是否已存在
    /// </summary>
    public bool ConfigExists(string name)
    {
        return _configs.Any(c => c.Name == name);
    }

    /// <summary>
    /// 重命名配置
    /// </summary>
    public bool RenameConfig(string oldName, string newName)
    {
        // 验证新名称
        if (string.IsNullOrWhiteSpace(newName))
        {
            Log.Warning("配置名称不能为空");
            return false;
        }

        if (oldName == newName)
        {
            return true; // 名称未变化，直接返回成功
        }

        // 检查新名称是否已存在
        if (ConfigExists(newName))
        {
            Log.Warning("配置名称 {NewName} 已存在", newName);
            return false;
        }

        // 查找要重命名的配置
        var config = _configs.FirstOrDefault(c => c.Name == oldName);
        if (config == null)
        {
            Log.Warning("未找到配置 {OldName}", oldName);
            return false;
        }

        // 执行重命名
        config.Name = newName;
        SaveToFile();
        Log.Information("配置已重命名: {OldName} -> {NewName}", oldName, newName);
        return true;
    }

    /// <summary>
    /// 从文件加载配置
    /// </summary>
    private void LoadConfigs()
    {
        try
        {
            if (!File.Exists(_configFilePath))
            {
                // 创建默认配置
                _configs = new List<AIConfig>
                {
                    new AIConfig
                    {
                        Name = "OpenAI 官方",
                        Model = "gpt-4o-mini",
                        BaseUrl = "https://api.openai.com/v1",
                        IsDefault = true
                    }
                };
                SaveToFile();
                Log.Information("创建默认 AI 配置");
                return;
            }

            var json = File.ReadAllText(_configFilePath);
            var configs = JsonSerializer.Deserialize<List<AIConfig>>(json);

            if (configs != null && configs.Count > 0)
            {
                _configs = configs;
                Log.Information("加载了 {Count} 个 AI 配置", _configs.Count);
            }
            else
            {
                // JSON 为空，使用默认配置
                _configs = new List<AIConfig>
                {
                    new AIConfig
                    {
                        Name = "OpenAI 官方",
                        Model = "gpt-4o-mini",
                        BaseUrl = "https://api.openai.com/v1",
                        IsDefault = true
                    }
                };
                SaveToFile();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载 AI 配置失败，使用默认配置");
            _configs = new List<AIConfig>
            {
                new AIConfig
                {
                    Name = "OpenAI 官方",
                    Model = "gpt-4o-mini",
                    BaseUrl = "https://api.openai.com/v1",
                    IsDefault = true
                }
            };
        }
    }

    /// <summary>
    /// 保存配置到文件
    /// </summary>
    private void SaveToFile()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            var json = JsonSerializer.Serialize(_configs, options);
            File.WriteAllText(_configFilePath, json);
            Log.Debug("AI 配置已保存到 {Path}", _configFilePath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "保存 AI 配置失败");
        }
    }
}
