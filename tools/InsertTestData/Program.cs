// 插入测试数据 - 模拟未结束的会话
using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime", "recordtime.db");

if (!File.Exists(dbPath))
{
    Console.WriteLine($"❌ 数据库文件不存在: {dbPath}");
    return 1;
}

Console.WriteLine("=== 测试 Phase 1 Task 1.1: 自动修复功能 ===");
Console.WriteLine($"📂 数据库路径: {dbPath}\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 1. 检查当前未结束的会话数量
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM Sessions WHERE EndTime IS NULL";
    var beforeCount = (long)cmd.ExecuteScalar()!;
    Console.WriteLine($"修复前: 未结束会话数 = {beforeCount}");
}

// 2. 插入2个测试用的未结束会话
Console.WriteLine("\n🔧 正在插入 2 个测试会话 (EndTime = NULL)...");

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
INSERT INTO Sessions (ProcessName, DisplayName, WindowTitleHash, StartTime, EndTime, DurationSeconds, ActivityType, Category, Confidence)
VALUES
('notepad', '记事本', 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855', datetime('now', '-10 minutes'), NULL, 0, 'ActiveTyping', '开发工具', 80),
('chrome', 'Google Chrome', 'e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855', datetime('now', '-15 minutes'), NULL, 0, 'PassiveBrowsing', '浏览器', 75)";

    var inserted = cmd.ExecuteNonQuery();
    Console.WriteLine($"✅ 已插入 {inserted} 条测试会话\n");
}

// 3. 再次检查未结束会话数量
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM Sessions WHERE EndTime IS NULL";
    var afterCount = (long)cmd.ExecuteScalar()!;
    Console.WriteLine($"插入后: 未结束会话数 = {afterCount}");
}

// 4. 显示未结束的会话详情
Console.WriteLine("\n📋 未结束的会话详情:");
Console.WriteLine("─────────────────────────────────────────────────────────────────────────");

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    ProcessName,
    DisplayName,
    StartTime,
    CAST((julianday('now') - julianday(StartTime)) * 24 * 60 AS INTEGER) as MinutesAgo
FROM Sessions
WHERE EndTime IS NULL
ORDER BY Id DESC
LIMIT 10";

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var id = reader.GetInt32(0);
        var processName = reader.GetString(1);
        var displayName = reader.GetString(2);
        var startTime = reader.GetDateTime(3);
        var minutesAgo = reader.GetInt32(4);

        Console.WriteLine($"ID: {id,5} | {displayName,-20} | 开始于: {startTime:yyyy-MM-dd HH:mm:ss} | {minutesAgo} 分钟前");
    }
}

Console.WriteLine("─────────────────────────────────────────────────────────────────────────");

Console.WriteLine("\n✅ 测试数据准备完成！");
Console.WriteLine("\n💡 下一步:");
Console.WriteLine("   1. 启动应用: dotnet run --project src/RecordTime.Avalonia");
Console.WriteLine("   2. 查看日志,确认自动修复是否执行");
Console.WriteLine("   3. 检查这些会话的 EndTime 是否已被设置\n");

return 0;
