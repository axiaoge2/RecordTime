// 检查特定会话的心跳详情
using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime", "recordtime.db");

Console.WriteLine("=== 心跳详情检查工具 ===\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 检查 ID 2216 的详细信息
Console.WriteLine("📊 会话 ID 2216 详情:");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    DisplayName,
    StartTime,
    EndTime,
    LastHeartbeat,
    DurationSeconds,
    CAST((julianday(LastHeartbeat) - julianday(StartTime)) * 86400 AS INTEGER) as HeartbeatDurationSeconds
FROM Sessions
WHERE Id = 2216";

    using var reader = cmd.ExecuteReader();

    if (reader.Read())
    {
        var id = reader.GetInt32(0);
        var displayName = reader.GetString(1);
        var startTime = reader.GetDateTime(2);
        var endTime = reader.GetDateTime(3);
        var lastHeartbeat = reader.GetDateTime(4);
        var durationSeconds = reader.GetInt32(5);
        var heartbeatDuration = reader.GetInt32(6);

        Console.WriteLine($"  ID: {id}");
        Console.WriteLine($"  应用: {displayName}");
        Console.WriteLine($"  开始时间: {startTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  结束时间: {endTime:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  最后心跳: {lastHeartbeat:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"  总时长: {durationSeconds} 秒 ({durationSeconds / 60} 分 {durationSeconds % 60} 秒)");
        Console.WriteLine($"  心跳时长: {heartbeatDuration} 秒 ({heartbeatDuration / 60} 分 {heartbeatDuration % 60} 秒)");

        // 计算心跳与结束时间的差异
        var diff = (endTime - lastHeartbeat).TotalSeconds;
        Console.WriteLine($"  心跳距离结束时间: {diff:F0} 秒");

        // 分析心跳更新情况
        Console.WriteLine($"\n  ✅ 分析:");
        if (heartbeatDuration > 30)
        {
            var expectedUpdates = heartbeatDuration / 30;
            Console.WriteLine($"  - 会话运行了约 {heartbeatDuration / 60} 分钟");
            Console.WriteLine($"  - 预期心跳更新次数: 约 {expectedUpdates} 次 (每30秒一次)");
            Console.WriteLine($"  - 最后一次心跳距离结束: {diff:F0} 秒");

            if (diff <= 30)
            {
                Console.WriteLine($"  - ✅ 心跳机制工作正常!最后心跳非常接近会话结束时间");
            }
            else
            {
                Console.WriteLine($"  - ⚠️  最后心跳距离结束时间超过30秒,可能心跳更新有延迟");
            }
        }
        else
        {
            Console.WriteLine($"  - 会话时长较短({heartbeatDuration}秒),心跳更新次数较少");
        }
    }
    else
    {
        Console.WriteLine("  未找到 ID 2216 的会话");
    }
}

Console.WriteLine("\n✅ 检查完成\n");
