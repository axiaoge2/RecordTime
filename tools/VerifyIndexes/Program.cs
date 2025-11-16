// 验证数据库索引 - 检查新创建的索引是否生效
using System;
using System.IO;
using Microsoft.Data.Sqlite;

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime", "recordtime.db");

Console.WriteLine("=== 数据库索引验证工具 ===");
Console.WriteLine($"📂 数据库路径: {dbPath}\n");

using var connection = new SqliteConnection($"Data Source={dbPath}");
connection.Open();

// 查询所有索引
Console.WriteLine("📊 Sessions 表的索引:");
Console.WriteLine(new string('─', 80));
Console.WriteLine($"{"索引名称",-40} {"索引列",-30} {"唯一性",-10}");
Console.WriteLine(new string('─', 80));

using (var cmd = connection.CreateCommand())
{
    cmd.CommandText = @"
SELECT
    name,
    sql
FROM sqlite_master
WHERE type = 'index'
  AND tbl_name = 'Sessions'
  AND name NOT LIKE 'sqlite_%'
ORDER BY name";

    using var reader = cmd.ExecuteReader();

    var indexCount = 0;
    var newIndexes = new[] { "IX_Sessions_EndTime", "IX_Sessions_LastHeartbeat", "IX_Sessions_StartTime_EndTime" };
    var foundNewIndexes = 0;

    while (reader.Read())
    {
        indexCount++;
        var indexName = reader.GetString(0);
        var sqlDef = reader.IsDBNull(1) ? "PRIMARY KEY" : reader.GetString(1);

        // 解析索引列
        var columns = "";
        if (sqlDef.Contains("(") && sqlDef.Contains(")"))
        {
            var start = sqlDef.IndexOf('(') + 1;
            var end = sqlDef.IndexOf(')');
            columns = sqlDef.Substring(start, end - start);
        }

        var isUnique = sqlDef.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) ? "是" : "否";

        // 检查是否是新索引
        var isNew = Array.Exists(newIndexes, x => x == indexName);
        var marker = isNew ? "✅ NEW" : "";

        if (isNew)
            foundNewIndexes++;

        Console.WriteLine($"{indexName,-40} {columns,-30} {isUnique,-10} {marker}");
    }

    Console.WriteLine(new string('─', 80));
    Console.WriteLine($"\n📈 索引统计:");
    Console.WriteLine($"   总索引数: {indexCount}");
    Console.WriteLine($"   新增索引数: {foundNewIndexes}/3");

    if (foundNewIndexes == 3)
    {
        Console.WriteLine($"\n✅ Phase 1 Task 1.3 索引创建成功!");
        Console.WriteLine($"   - IX_Sessions_EndTime (用于 NULL 查询优化)");
        Console.WriteLine($"   - IX_Sessions_LastHeartbeat (用于过期会话检测)");
        Console.WriteLine($"   - IX_Sessions_StartTime_EndTime (用于日期范围查询优化)");
    }
    else
    {
        Console.WriteLine($"\n⚠️  发现 {foundNewIndexes}/3 个新索引，可能需要重新应用 Migration");
    }
}

Console.WriteLine("\n✅ 验证完成\n");
return 0;
