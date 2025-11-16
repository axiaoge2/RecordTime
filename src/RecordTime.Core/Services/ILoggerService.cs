using System;

namespace RecordTime.Core.Services;

/// <summary>
/// 日志服务接口
/// </summary>
public interface ILoggerService
{
    /// <summary>
    /// 记录详细调试信息 (Verbose)
    /// </summary>
    void Verbose(string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// 记录调试信息 (Debug)
    /// </summary>
    void Debug(string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// 记录一般信息 (Information)
    /// </summary>
    void Information(string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// 记录警告信息 (Warning)
    /// </summary>
    void Warning(string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// 记录错误信息 (Error)
    /// </summary>
    void Error(Exception? exception, string messageTemplate, params object[] propertyValues);

    /// <summary>
    /// 记录致命错误 (Fatal)
    /// </summary>
    void Fatal(Exception? exception, string messageTemplate, params object[] propertyValues);
}
