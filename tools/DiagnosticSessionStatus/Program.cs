using Microsoft.EntityFrameworkCore;
using RecordTime.Data;
using RecordTime.Core.Models;

Console.WriteLine("=== RecordTime 会话状态诊断工具 ===\n");

var dbPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "RecordTime",
    "recordtime.db"
);

Console.WriteLine($"数据库路径: {dbPath}");
if (!File.Exists(dbPath))
{
    Console.WriteLine("错误: 数据库文件不存在!");
    return;
}

using var db = new RecordTimeDbContext();

// 1. 检查未结束的会话
var unfinishedSessions = await db.Sessions
    .Where(s => s.EndTime == null)
    .OrderBy(s => s.StartTime)
    .ToListAsync();

Console.WriteLine($"\n未结束的会话数量: {unfinishedSessions.Count}");

if (unfinishedSessions.Any())
{
    Console.WriteLine("\n详细信息:");
    Console.WriteLine(new string('-', 120));
    Console.WriteLine($"{"ID",-8} {"进程名称",-20} {"开始时间",-25} {"最后心跳",-25} {"距今(分钟)",-15}");
    Console.WriteLine(new string('-', 120));

    var now = DateTime.Now;
    foreach (var session in unfinishedSessions)
    {
        var minutesSinceStart = (now - session.StartTime).TotalMinutes;
        var lastHeartbeat = session.LastHeartbeat?.ToString("yyyy-MM-dd HH:mm:ss") ?? "无";

        Console.WriteLine($"{session.Id,-8} {session.ProcessName,-20} {session.StartTime:yyyy-MM-dd HH:mm:ss} {lastHeartbeat,-25} {minutesSinceStart,-15:F1}");
    }
}

// 2. 检查过期的会话 (LastHeartbeat > 2分钟)
var twoMinutesAgo = DateTime.Now.AddMinutes(-2);
var expiredSessions = await db.Sessions
    .Where(s => s.EndTime == null && s.LastHeartbeat != null && s.LastHeartbeat < twoMinutesAgo)
    .ToListAsync();

Console.WriteLine($"\n\n过期会话 (LastHeartbeat > 2分钟): {expiredSessions.Count}");

if (expiredSessions.Any())
{
    Console.WriteLine("\n这些会话应该被自动结束:");
    foreach (var session in expiredSessions)
    {
        var minutesSinceHeartbeat = (DateTime.Now - session.LastHeartbeat!.Value).TotalMinutes;
        Console.WriteLine($"  - ID {session.Id}: {session.ProcessName}, 最后心跳: {minutesSinceHeartbeat:F1} 分钟前");
    }
}

// 3. 检查今天的会话统计
var todayStart = DateTime.Today;
var todayEnd = todayStart.AddDays(1);

var todaySessions = await db.Sessions
    .Where(s => s.StartTime >= todayStart && s.StartTime < todayEnd)
    .ToListAsync();

Console.WriteLine($"\n\n今天的会话统计 ({todayStart:yyyy-MM-dd}):");
Console.WriteLine($"  总会话数: {todaySessions.Count}");
Console.WriteLine($"  已结束: {todaySessions.Count(s => s.EndTime != null)}");
Console.WriteLine($"  未结束: {todaySessions.Count(s => s.EndTime == null)}");

var finishedToday = todaySessions.Where(s => s.EndTime != null).ToList();
if (finishedToday.Any())
{
    var totalSeconds = finishedToday.Sum(s => s.DurationSeconds);
    var hours = totalSeconds / 3600;
    var minutes = (totalSeconds % 3600) / 60;
    Console.WriteLine($"  已记录总时长: {hours:F0}小时 {minutes:F0}分钟");
}

// 4. 显示最近的5个会话
Console.WriteLine("\n\n最近的5个会话:");
var recentSessions = await db.Sessions
    .OrderByDescending(s => s.StartTime)
    .Take(5)
    .ToListAsync();

Console.WriteLine(new string('-', 140));
Console.WriteLine($"{"ID",-8} {"进程名称",-20} {"开始时间",-20} {"结束时间",-20} {"时长",-15} {"最后心跳",-20}");
Console.WriteLine(new string('-', 140));

foreach (var session in recentSessions)
{
    var duration = session.EndTime.HasValue
        ? $"{session.DurationSeconds / 60:F0}分钟"
        : "进行中";
    var endTimeStr = session.EndTime?.ToString("MM-dd HH:mm:ss") ?? "-";
    var heartbeatStr = session.LastHeartbeat?.ToString("MM-dd HH:mm:ss") ?? "-";

    Console.WriteLine($"{session.Id,-8} {session.ProcessName,-20} {session.StartTime:MM-dd HH:mm:ss,-20} {endTimeStr,-20} {duration,-15} {heartbeatStr,-20}");
}

Console.WriteLine("\n\n诊断完成。");
Console.WriteLine("\n建议:");

if (expiredSessions.Any())
{
    Console.WriteLine("⚠ 发现过期会话未被自动结束，可能需要手动修复。");
    Console.WriteLine("  可以重启应用让自动修复功能运行，或使用 DatabaseCleanup 工具手动修复。");
}

if (unfinishedSessions.Any() && !expiredSessions.Any())
{
    Console.WriteLine("ℹ 有未结束的会话，但它们的心跳正常（< 2分钟前）。");
    Console.WriteLine("  如果监控服务正在运行，这是正常的。");
}

if (!unfinishedSessions.Any())
{
    Console.WriteLine("✓ 没有未结束的会话，数据状态正常。");
}
