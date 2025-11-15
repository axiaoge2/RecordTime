using RecordTime.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace RecordTime.Console;

/// <summary>
/// 验证数据库数据
/// </summary>
class VerifyData
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("正在查询数据库...\n");

        using var dbContext = new RecordTimeDbContext();

        // 获取今天的会话
        var today = DateTime.Today;
        var sessions = await dbContext.Sessions
            .Where(s => s.StartTime >= today)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        System.Console.WriteLine($"今天共记录 {sessions.Count} 个会话\n");

        if (sessions.Count > 0)
        {
            System.Console.WriteLine("最近10条记录:");
            System.Console.WriteLine(new string('-', 80));

            foreach (var session in sessions.TakeLast(10))
            {
                System.Console.WriteLine($"应用: {session.ProcessName,-20} | 分类: {session.Category,-10}");
                System.Console.WriteLine($"活动: {session.ActivityType,-20} | 置信度: {session.Confidence}%");
                System.Console.WriteLine($"开始: {session.StartTime:HH:mm:ss} | 结束: {session.EndTime:HH:mm:ss} | 时长: {session.DurationSeconds}秒");
                System.Console.WriteLine(new string('-', 80));
            }

            // 统计
            var totalDuration = sessions.Sum(s => s.DurationSeconds);
            var categoryStats = sessions
                .GroupBy(s => s.Category ?? "未分类")
                .Select(g => new { Category = g.Key, Duration = g.Sum(s => s.DurationSeconds) })
                .OrderByDescending(x => x.Duration);

            System.Console.WriteLine("\n分类统计:");
            foreach (var stat in categoryStats)
            {
                var duration = TimeSpan.FromSeconds(stat.Duration);
                System.Console.WriteLine($"  {stat.Category,-15} {duration:hh\\:mm\\:ss}");
            }

            System.Console.WriteLine($"\n总时长: {TimeSpan.FromSeconds(totalDuration):hh\\:mm\\:ss}");
        }
        else
        {
            System.Console.WriteLine("数据库为空");
        }
    }
}
