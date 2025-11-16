namespace RecordTime.Core.Exceptions;

/// <summary>
/// 监控服务相关异常
/// </summary>
public class MonitoringException : RecordTimeException
{
    public MonitoringException(
        string message,
        string userMessage = "监控服务出现问题，请重启监控",
        Exception? innerException = null)
        : base("MONITOR_ERROR", message, userMessage, isRecoverable: true, innerException)
    {
    }

    /// <summary>
    /// 监控服务启动失败
    /// </summary>
    public static MonitoringException StartupFailed(string serviceName, Exception? innerException = null)
    {
        return new MonitoringException(
            $"监控服务 {serviceName} 启动失败",
            $"{serviceName} 启动失败，请检查系统权限或重启应用",
            innerException);
    }

    /// <summary>
    /// 监控服务运行时错误
    /// </summary>
    public static MonitoringException RuntimeError(string serviceName, Exception? innerException = null)
    {
        return new MonitoringException(
            $"监控服务 {serviceName} 运行时错误",
            "监控服务遇到问题，已自动尝试恢复",
            innerException);
    }

    /// <summary>
    /// 会话管理错误
    /// </summary>
    public static MonitoringException SessionError(string operation, Exception? innerException = null)
    {
        return new MonitoringException(
            $"会话操作失败: {operation}",
            "会话记录失败，部分数据可能丢失",
            innerException);
    }
}
