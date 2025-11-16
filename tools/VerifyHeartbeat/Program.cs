// 验证心跳机制 - 查看最近会话的心跳记录
using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime", "recordtime.db");

Console.WriteLine("=== 心跳机制验证工具 ===");
Console.WriteLine($"📂 数据库路径: {dbPath}\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 1. 检查数据库 schema 是否包含 LastHeartbeat 列
Console.WriteLine("🔍 检查数据库 schema...");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "PRAGMA table_info(Sessions)";
    using var reader = cmd.ExecuteReader();

    var hasLastHeartbeat = false;
    while (reader.Read())
    {
        var columnName = reader.GetString(1);
        if (columnName == "LastHeartbeat")
        {
            hasLastHeartbeat = true;
            Console.WriteLine($"   ✅ LastHeartbeat 列已存在");
            break;
        }
    }

    if (!hasLastHeartbeat)
    {
        Console.WriteLine($"   ❌ LastHeartbeat 列不存在，请先应用 Migration");
        return 1;
    }
}

// 2. 查看最近的会话记录（包括心跳信息）
Console.WriteLine("\n📊 最近 10 条会话记录:");
Console.WriteLine(new string('─', 120));
Console.WriteLine($"{"ID",-6} {"进程名",-20} {"开始时间",-20} {"结束时间",-20} {"最后心跳",-20} {"时长",-8} {"状态",-10}");
Console.WriteLine(new string('─', 120));

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    ProcessName,
    DisplayName,
    StartTime,
    EndTime,
    LastHeartbeat,
    DurationSeconds
FROM Sessions
ORDER BY Id DESC
LIMIT 10";

    using var reader = cmd.ExecuteReader();

    var totalRecords = 0;
    var recordsWithHeartbeat = 0;
    var activeRecords = 0;

    while (reader.Read())
    {
        totalRecords++;

        var id = reader.GetInt32(0);
        var processName = reader.GetString(1);
        var displayName = reader.GetString(2);
        var startTime = reader.GetDateTime(3);
        var endTime = reader.IsDBNull(4) ? (DateTime?)null : reader.GetDateTime(4);
        var lastHeartbeat = reader.IsDBNull(5) ? (DateTime?)null : reader.GetDateTime(5);
        var durationSeconds = reader.GetInt32(6);

        if (lastHeartbeat != null)
            recordsWithHeartbeat++;

        if (endTime == null)
            activeRecords++;

        var startTimeStr = startTime.ToString("yyyy-MM-dd HH:mm:ss");
        var endTimeStr = endTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? "null";
        var heartbeatStr = lastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "null";
        var durationStr = $"{durationSeconds}s";
        var status = endTime == null ? "未结束" : "已结束";

        Console.WriteLine($"{id,-6} {displayName,-20} {startTimeStr,-20} {endTimeStr,-20} {heartbeatStr,-20} {durationStr,-8} {status,-10}");
    }

    Console.WriteLine(new string('─', 120));
    Console.WriteLine($"\n📈 统计:");
    Console.WriteLine($"   总记录数: {totalRecords}");
    Console.WriteLine($"   有心跳记录: {recordsWithHeartbeat} ({(totalRecords > 0 ? recordsWithHeartbeat * 100.0 / totalRecords : 0):F1}%)");
    Console.WriteLine($"   未结束会话: {activeRecords}");
}

// 3. 检查今天是否有心跳更新
Console.WriteLine("\n🔍 检查今天的会话心跳:");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    DisplayName,
    StartTime,
    LastHeartbeat,
    CAST((julianday(LastHeartbeat) - julianday(StartTime)) * 24 * 60 AS INTEGER) as HeartbeatMinutes
FROM Sessions
WHERE date(StartTime) = date('now')
  AND LastHeartbeat IS NOT NULL
ORDER BY StartTime DESC
LIMIT 5";

    using var reader = cmd.ExecuteReader();

    var hasResults = false;
    while (reader.Read())
    {
        if (!hasResults)
        {
            Console.WriteLine($"\n{"ID",-6} {"应用",-20} {"开始时间",-20} {"最后心跳",-20} {"心跳时长",-10}");
            Console.WriteLine(new string('─', 80));
            hasResults = true;
        }

        var id = reader.GetInt32(0);
        var displayName = reader.GetString(1);
        var startTime = reader.GetDateTime(2);
        var lastHeartbeat = reader.GetDateTime(3);
        var heartbeatMinutes = reader.GetInt32(4);

        Console.WriteLine($"{id,-6} {displayName,-20} {startTime:HH:mm:ss,-20} {lastHeartbeat:HH:mm:ss,-20} {heartbeatMinutes,-10} 分钟");
    }

    if (!hasResults)
    {
        Console.WriteLine("   今天没有带心跳的会话记录");
    }
}

// 4. 检查是否有需要修复的过期会话
Console.WriteLine("\n⚠️  检查过期会话 (LastHeartbeat 超过 2 分钟):");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    DisplayName,
    StartTime,
    LastHeartbeat,
    CAST((julianday('now') - julianday(LastHeartbeat)) * 24 * 60 AS INTEGER) as MinutesSinceHeartbeat
FROM Sessions
WHERE EndTime IS NULL
  AND (LastHeartbeat IS NULL OR LastHeartbeat < datetime('now', '-2 minutes'))
ORDER BY StartTime DESC";

    using var reader = cmd.ExecuteReader();

    var staleCount = 0;
    while (reader.Read())
    {
        if (staleCount == 0)
        {
            Console.WriteLine($"\n{"ID",-6} {"应用",-20} {"开始时间",-20} {"最后心跳",-20} {"距今分钟",-10}");
            Console.WriteLine(new string('─', 80));
        }

        staleCount++;

        var id = reader.GetInt32(0);
        var displayName = reader.GetString(1);
        var startTime = reader.GetDateTime(2);
        var lastHeartbeat = reader.IsDBNull(3) ? (DateTime?)null : reader.GetDateTime(3);
        var minutesSinceHeartbeat = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);

        var heartbeatStr = lastHeartbeat?.ToString("HH:mm:ss") ?? "无心跳";
        Console.WriteLine($"{id,-6} {displayName,-20} {startTime:HH:mm:ss,-20} {heartbeatStr,-20} {minutesSinceHeartbeat,-10}");
    }

    if (staleCount == 0)
    {
        Console.WriteLine("   ✅ 没有发现过期会话");
    }
    else
    {
        Console.WriteLine($"\n   ⚠️  发现 {staleCount} 个过期会话，下次启动应用时会自动修复");
    }
}

Console.WriteLine("\n✅ 验证完成\n");
return 0;
