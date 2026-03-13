using Microsoft.Extensions.Configuration;
using RecordTime.Core.Services;
using System;
using System.IO;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 配置服务实现 - 单例模式
/// </summary>
public class ConfigurationService : IConfigurationService
{
    private IConfiguration _configuration;
    private AppConfiguration _currentConfig;

    public AppConfiguration Current => _currentConfig;

    public ConfigurationService()
    {
        _configuration = BuildConfiguration();
        _currentConfig = LoadConfiguration();
    }

    public void Reload()
    {
        try
        {
            _configuration = BuildConfiguration();
            _currentConfig = LoadConfiguration();
            Log.Information("配置已重新加载");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "重新加载配置失败");
        }
    }

    private IConfiguration BuildConfiguration()
    {
        // 获取应用程序所在目录
        var appDirectory = AppContext.BaseDirectory;

        var builder = new ConfigurationBuilder()
            .SetBasePath(appDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

        return builder.Build();
    }

    private AppConfiguration LoadConfiguration()
    {
        try
        {
            var config = new AppConfiguration();
            _configuration.Bind(config);

            Log.Debug("配置加载成功: WindowPollIntervalMs={WindowPollInterval}, DataRefreshIntervalMs={DataRefreshInterval}",
                config.Monitoring.WindowPollIntervalMs,
                config.Monitoring.DataRefreshIntervalMs);

            return config;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载配置失败，使用默认配置");
            return new AppConfiguration();
        }
    }
}
