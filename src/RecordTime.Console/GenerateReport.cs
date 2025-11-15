using RecordTime.Data;
using RecordTime.Core.Services;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace RecordTime.Console;

/// <summary>
/// 生成可视化HTML报告
/// </summary>
class GenerateReport
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("📊 RecordTime 报告生成器\n");

        // 选择日期
        DateTime targetDate;
        if (args.Length > 0 && DateTime.TryParse(args[0], out var date))
        {
            targetDate = date.Date;
        }
        else
        {
            targetDate = DateTime.Today;
        }

        System.Console.WriteLine($"生成日期: {targetDate:yyyy-MM-dd}\n");

        // 查询数据
        using var dbContext = new RecordTimeDbContext();

        var sessions = await dbContext.Sessions
            .Where(s => s.StartTime >= targetDate && s.StartTime < targetDate.AddDays(1))
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            System.Console.WriteLine("❌ 该日期没有数据记录");
            return;
        }

        System.Console.WriteLine($"✅ 找到 {sessions.Count} 条会话记录");
        System.Console.WriteLine($"⏱️  总时长: {TimeSpan.FromSeconds(sessions.Sum(s => s.DurationSeconds)):hh\\:mm\\:ss}\n");

        // 生成报告
        var reportGenerator = new ReportGenerator();
        var outputDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime",
            "Reports"
        );

        Directory.CreateDirectory(outputDir);

        var fileName = $"RecordTime_Report_{targetDate:yyyyMMdd}.html";
        var outputPath = Path.Combine(outputDir, fileName);

        System.Console.WriteLine("📝 正在生成报告...");

        try
        {
            var reportPath = reportGenerator.GenerateDailyReport(sessions, targetDate, outputPath);

            System.Console.WriteLine($"✅ 报告已生成！");
            System.Console.WriteLine($"📁 文件位置: {reportPath}\n");

            // 询问是否打开
            System.Console.Write("是否在浏览器中打开报告? (Y/n): ");
            var response = System.Console.ReadLine()?.Trim().ToLower();

            if (string.IsNullOrEmpty(response) || response == "y" || response == "yes")
            {
                System.Console.WriteLine("🌐 正在打开浏览器...");

                // 在默认浏览器中打开
                var psi = new ProcessStartInfo
                {
                    FileName = reportPath,
                    UseShellExecute = true
                };
                Process.Start(psi);

                System.Console.WriteLine("✅ 已在浏览器中打开报告");
            }
            else
            {
                System.Console.WriteLine($"💡 提示: 手动打开文件即可查看报告");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ 生成报告失败: {ex.Message}");
            System.Console.WriteLine($"   {ex.StackTrace}");
        }
    }
}
