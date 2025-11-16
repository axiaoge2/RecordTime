// 验证自动修复功能 - 检查之前的测试会话是否已被修复
using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime", "recordtime.db");

Console.WriteLine("=== 验证 Phase 1 Task 1.1: 自动修复功能 ===");
Console.WriteLine($"📂 数据库路径: {dbPath}\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 1. 检查当前未结束的会话数量
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM Sessions WHERE EndTime IS NULL";
    var count = (long)cmd.ExecuteScalar()!;
    Console.WriteLine($"✅ 当前未结束会话数: {count}");

    if (count == 0)
    {
        Console.WriteLine("   太好了!所有会话都已正确结束。\n");
    }
    else
    {
        Console.WriteLine($"   ⚠️  警告:仍有 {count} 个未结束的会话\n");
    }
}

// 2. 查看最近插入的测试会话 (ID >= 2213)
Console.WriteLine("📋 最近插入的测试会话状态:");
Console.WriteLine("─────────────────────────────────────────────────────────────────────────");

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    ProcessName,
    DisplayName,
    StartTime,
    EndTime,
    DurationSeconds
FROM Sessions
WHERE Id >= 2213
ORDER BY Id";

    using var reader = cmd.ExecuteReader();

    var hasData = false;
    while (reader.Read())
    {
        hasData = true;
        var id = reader.GetInt32(0);
        var processName = reader.GetString(1);
        var displayName = reader.GetString(2);
        var startTime = reader.GetDateTime(3);
        var endTime = reader.IsDBNull(4) ? "NULL ❌" : reader.GetDateTime(4).ToString("yyyy-MM-dd HH:mm:ss") + " ✅";
        var duration = reader.GetInt32(5);

        Console.WriteLine($"ID: {id,5} | {displayName,-20}");
        Console.WriteLine($"         开始: {startTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"         结束: {endTime}");
        Console.WriteLine($"         时长: {duration}秒 ({duration/60}分钟)");
        Console.WriteLine();
    }

    if (!hasData)
    {
        Console.WriteLine("   没有找到ID >= 2213的会话");
    }
}

Console.WriteLine("─────────────────────────────────────────────────────────────────────────");

// 3. 验证自动修复逻辑
Console.WriteLine("\n🔍 验证自动修复逻辑:");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    ProcessName,
    DisplayName,
    StartTime,
    EndTime,
    DurationSeconds,
    CAST((julianday(EndTime) - julianday(StartTime)) * 24 * 60 AS INTEGER) as ActualMinutes
FROM Sessions
WHERE Id >= 2213 AND EndTime IS NOT NULL";

    using var reader = cmd.ExecuteReader();

    var allCorrect = true;
    while (reader.Read())
    {
        var id = reader.GetInt32(0);
        var displayName = reader.GetString(2);
        var duration = reader.GetInt32(5);
        var actualMinutes = reader.GetInt32(6);

        var isCorrect = duration == 300 && actualMinutes == 5;
        var status = isCorrect ? "✅ 正确" : "❌ 错误";

        Console.WriteLine($"   ID {id} ({displayName}): 时长={duration}秒, 实际={actualMinutes}分钟 {status}");

        if (!isCorrect)
        {
            allCorrect = false;
        }
    }

    if (allCorrect)
    {
        Console.WriteLine("\n✅ 所有测试会话都已正确修复为 5分钟(300秒)!");
    }
}

Console.WriteLine("\n✅ 验证完成!\n");

return 0;
