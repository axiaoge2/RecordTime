namespace RecordTime.Core.Exceptions;

/// <summary>
/// RecordTime 应用的基础异常类
/// </summary>
public class RecordTimeException : Exception
{
    /// <summary>
    /// 错误码（用于客户端识别特定错误）
    /// </summary>
    public string ErrorCode { get; }

    /// <summary>
    /// 是否为可恢复的错误
    /// </summary>
    public bool IsRecoverable { get; }

    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public string UserMessage { get; }

    public RecordTimeException(
        string errorCode,
        string message,
        string userMessage,
        bool isRecoverable = false,
        Exception? innerException = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage;
        IsRecoverable = isRecoverable;
    }
}
