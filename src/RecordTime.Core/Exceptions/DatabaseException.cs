namespace RecordTime.Core.Exceptions;

/// <summary>
/// 数据库相关异常
/// </summary>
public class DatabaseException : RecordTimeException
{
    public DatabaseException(
        string message,
        string userMessage = "数据库操作失败，请稍后重试",
        Exception? innerException = null)
        : base("DB_ERROR", message, userMessage, isRecoverable: true, innerException)
    {
    }

    /// <summary>
    /// 数据库连接失败
    /// </summary>
    public static DatabaseException ConnectionFailed(Exception? innerException = null)
    {
        return new DatabaseException(
            "无法连接到数据库",
            "数据库连接失败，请检查数据库文件是否存在或被占用",
            innerException);
    }

    /// <summary>
    /// 数据库迁移失败
    /// </summary>
    public static DatabaseException MigrationFailed(Exception? innerException = null)
    {
        return new DatabaseException(
            "数据库迁移失败",
            "数据库结构更新失败，请尝试重启应用或联系技术支持",
            innerException);
    }

    /// <summary>
    /// 数据操作失败
    /// </summary>
    public static DatabaseException OperationFailed(string operation, Exception? innerException = null)
    {
        return new DatabaseException(
            $"数据库操作失败: {operation}",
            "数据保存失败，请稍后重试",
            innerException);
    }
}
