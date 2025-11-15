using System;
using System.Linq;
using System.Threading.Tasks;
using RecordTime.Data;
using Microsoft.EntityFrameworkCore;

namespace RecordTime.Console;

/// <summary>
/// 清空今天的使用时间数据
/// </summary>
public class ClearTodayData
{
    public static async Task Main(string[] args)
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;
        System.Console.WriteLine("=== 清空今天的使用时间数据 ===");
        System.Console.WriteLine();

        try
        {
            await using var dbContext = new RecordTimeDbContext();

            // 查询今天的记录数
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var todaySessions = await dbContext.Sessions
                .Where(s => s.StartTime >= today && s.StartTime < tomorrow)
                .ToListAsync();

            System.Console.WriteLine($"今天共有 {todaySessions.Count} 条会话记录");

            if (todaySessions.Count == 0)
            {
                System.Console.WriteLine("✅ 今天没有数据，无需清空");
                return;
            }

            // 显示前5条记录
            System.Console.WriteLine();
            System.Console.WriteLine("前5条记录:");
            foreach (var session in todaySessions.Take(5))
            {
                System.Console.WriteLine($"  - {session.DisplayName} ({session.StartTime:HH:mm:ss} - {session.EndTime?.ToString("HH:mm:ss") ?? "未结束"})");
            }

            // 自动确认删除(临时修改用于修复bug)
            System.Console.WriteLine();
            System.Console.WriteLine("⚠️  自动确认删除,清除未结束的会话记录...");
            var confirm = "YES";

            // if (confirm != "YES")
            // {
            //     System.Console.WriteLine("❌ 已取消操作");
            //     return;
            // }

            // 删除记录
            System.Console.WriteLine();
            System.Console.WriteLine("正在删除记录...");

            dbContext.Sessions.RemoveRange(todaySessions);
            var deleted = await dbContext.SaveChangesAsync();

            System.Console.WriteLine($"✅ 成功删除 {deleted} 条记录");
            System.Console.WriteLine();
            System.Console.WriteLine("提示: 请重新打开 RecordTime 应用以刷新界面");
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ 删除失败: {ex.Message}");
            System.Console.WriteLine($"   {ex.StackTrace}");
        }
    }
}
