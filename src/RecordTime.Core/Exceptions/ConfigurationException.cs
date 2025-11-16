namespace RecordTime.Core.Exceptions;

/// <summary>
/// 配置相关异常
/// </summary>
public class ConfigurationException : RecordTimeException
{
    public ConfigurationException(
        string message,
        string userMessage = "配置加载失败，将使用默认配置",
        Exception? innerException = null)
        : base("CONFIG_ERROR", message, userMessage, isRecoverable: true, innerException)
    {
    }

    /// <summary>
    /// 配置文件缺失
    /// </summary>
    public static ConfigurationException FileMissing(string filePath)
    {
        return new ConfigurationException(
            $"配置文件不存在: {filePath}",
            "配置文件缺失，将使用默认配置");
    }

    /// <summary>
    /// 配置文件格式错误
    /// </summary>
    public static ConfigurationException InvalidFormat(string filePath, Exception? innerException = null)
    {
        return new ConfigurationException(
            $"配置文件格式错误: {filePath}",
            "配置文件格式不正确，将使用默认配置",
            innerException);
    }

    /// <summary>
    /// 配置值无效
    /// </summary>
    public static ConfigurationException InvalidValue(string key, string value)
    {
        return new ConfigurationException(
            $"配置项 {key} 的值 '{value}' 无效",
            $"配置项 {key} 设置不正确，将使用默认值");
    }
}
