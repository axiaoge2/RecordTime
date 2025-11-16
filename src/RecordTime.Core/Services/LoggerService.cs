using System;
using Serilog;

namespace RecordTime.Core.Services;

/// <summary>
/// Serilog 日志服务实现
/// </summary>
public class LoggerService : ILoggerService
{
    private readonly ILogger _logger;

    public LoggerService()
    {
        // 如果 Log.Logger 还未初始化,创建一个默认的logger
        _logger = Log.Logger ?? Log.ForContext<LoggerService>();
    }

    public LoggerService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Verbose(string messageTemplate, params object[] propertyValues)
    {
        _logger.Verbose(messageTemplate, propertyValues);
    }

    public void Debug(string messageTemplate, params object[] propertyValues)
    {
        _logger.Debug(messageTemplate, propertyValues);
    }

    public void Information(string messageTemplate, params object[] propertyValues)
    {
        _logger.Information(messageTemplate, propertyValues);
    }

    public void Warning(string messageTemplate, params object[] propertyValues)
    {
        _logger.Warning(messageTemplate, propertyValues);
    }

    public void Error(Exception? exception, string messageTemplate, params object[] propertyValues)
    {
        _logger.Error(exception, messageTemplate, propertyValues);
    }

    public void Fatal(Exception? exception, string messageTemplate, params object[] propertyValues)
    {
        _logger.Fatal(exception, messageTemplate, propertyValues);
    }
}
