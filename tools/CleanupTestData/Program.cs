// 清理测试数据 - 删除模拟的测试会话
using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime", "recordtime.db");

Console.WriteLine("=== 清理测试数据 ===");
Console.WriteLine($"📂 数据库路径: {dbPath}\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 1. 检查测试数据
Console.WriteLine("🔍 检查测试数据 (ID >= 2213)...");

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    Id,
    ProcessName,
    DisplayName,
    StartTime
FROM Sessions
WHERE Id >= 2213
ORDER BY Id";

    using var reader = cmd.ExecuteReader();

    var count = 0;
    while (reader.Read())
    {
        count++;
        var id = reader.GetInt32(0);
        var processName = reader.GetString(1);
        var displayName = reader.GetString(2);
        var startTime = reader.GetDateTime(3);

        Console.WriteLine($"   ID: {id,5} | {displayName,-20} | {processName,-15} | {startTime:yyyy-MM-dd HH:mm:ss}");
    }

    if (count == 0)
    {
        Console.WriteLine("   没有找到测试数据");
        Console.WriteLine("\n✅ 数据库已经是干净的!");
        return 0;
    }

    Console.WriteLine($"\n找到 {count} 条测试记录");
}

// 2. 确认删除
Console.Write("\n是否删除这些测试数据? (Y/N): ");
var response = Console.ReadLine();

if (response?.ToUpper() != "Y")
{
    Console.WriteLine("⏹️ 已取消操作");
    return 0;
}

// 3. 删除测试数据
Console.WriteLine("\n🗑️ 正在删除测试数据...");

int deletedCount;
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "DELETE FROM Sessions WHERE Id >= 2213";
    deletedCount = cmd.ExecuteNonQuery();
}

Console.WriteLine($"✅ 已删除 {deletedCount} 条测试记录");

// 4. 验证删除
using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = "SELECT COUNT(*) FROM Sessions WHERE Id >= 2213";
    var remaining = (long)cmd.ExecuteScalar()!;

    if (remaining == 0)
    {
        Console.WriteLine("✅ 验证成功: 所有测试数据已被删除\n");
    }
    else
    {
        Console.WriteLine($"⚠️  警告: 仍有 {remaining} 条记录未删除\n");
    }
}

return 0;
