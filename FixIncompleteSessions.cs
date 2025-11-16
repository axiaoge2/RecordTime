// 快速修复异常会话的 C# 脚本
// 运行: dotnet script FixIncompleteSessions.cs
// 或者: csc FixIncompleteSessions.cs && FixIncompleteSessions.exe

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

Console.WriteLine($"📂 数据库路径: {dbPath}");
Console.WriteLine();

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 1. 查询异常会话数量
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM Sessions WHERE EndTime IS NULL";
    var count = (long)cmd.ExecuteScalar()!;
    Console.WriteLine($"🔍 发现 {count} 条未结束的会话");

    if (count == 0)
    {
        Console.WriteLine("✅ 数据库中没有异常会话,无需清理");
        return 0;
    }
}

// 2. 显示异常会话详情
Console.WriteLine("\n📋 异常会话详情 (最近10条):");
Console.WriteLine("─────────────────────────────────────────────────────────────");
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
        SELECT
            Id,
            ProcessName,
            DisplayName,
            StartTime,
            CAST((julianday('now') - julianday(StartTime)) * 24 AS INTEGER) as HoursSinceStart
        FROM Sessions
        WHERE EndTime IS NULL
        ORDER BY StartTime DESC
        LIMIT 10";

    using var reader = cmd.ExecuteReader();
    while (reader.Read())
    {
        var id = reader.GetInt32(0);
        var processName = reader.GetString(1);
        var displayName = reader.IsDBNull(2) ? "N/A" : reader.GetString(2);
        var startTime = reader.GetDateTime(3);
        var hoursSince = reader.GetInt32(4);

        Console.WriteLine($"ID: {id,5} | {displayName,-20} | {startTime:yyyy-MM-dd HH:mm} | {hoursSince}h 前");
    }
}

Console.WriteLine("─────────────────────────────────────────────────────────────");
Console.Write("\n是否修复这些异常会话? (Y/N): ");
var response = Console.ReadLine();

if (response?.ToUpper() != "Y")
{
    Console.WriteLine("⏹️ 已取消操作");
    return 0;
}

// 3. 备份数据库
var backupPath = Path.Combine(
    Path.GetDirectoryName(dbPath)!,
    $"recordtime_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db");

File.Copy(dbPath, backupPath);
Console.WriteLine($"\n✅ 已创建备份: {backupPath}");

// 4. 修复异常会话
Console.WriteLine("\n🔧 正在修复异常会话...");

int fixedCount;
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
        UPDATE Sessions
        SET EndTime = datetime(StartTime, '+5 minutes'),
            DurationSeconds = 300
        WHERE EndTime IS NULL";

    fixedCount = cmd.ExecuteNonQuery();
}

Console.WriteLine($"✅ 修复完成! 已处理 {fixedCount} 条异常会话");
Console.WriteLine("   所有会话的 EndTime 已设置为 StartTime + 5分钟");
Console.WriteLine($"\n💡 如需恢复,请使用备份文件:\n   {backupPath}");

return 0;
