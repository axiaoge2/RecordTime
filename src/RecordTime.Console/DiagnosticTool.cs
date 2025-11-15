using RecordTime.Data;
using RecordTime.Core.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RecordTime.Console;

public class DiagnosticTool
{
    public static async Task RunDiagnostics()
    {
        System.Console.WriteLine("=== RecordTime 诊断工具 ===\n");

        // 1. 检查数据库路径
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime",
            "recordtime.db"
        );
        System.Console.WriteLine($"1. 数据库路径: {dbPath}");
        System.Console.WriteLine($"   数据库存在: {File.Exists(dbPath)}");

        if (File.Exists(dbPath))
        {
            var fileInfo = new FileInfo(dbPath);
            System.Console.WriteLine($"   文件大小: {fileInfo.Length} bytes");
            System.Console.WriteLine($"   最后修改: {fileInfo.LastWriteTime}");
        }

        // 2. 测试数据库连接
        System.Console.WriteLine("\n2. 测试数据库连接...");
        try
        {
            await using var dbContext = new RecordTimeDbContext();
            var count = await dbContext.Sessions.CountAsync();
            System.Console.WriteLine($"   ✅ 数据库连接成功");
            System.Console.WriteLine($"   会话记录数: {count}");

            // 显示最近5条记录
            var recentSessions = await dbContext.Sessions
                .OrderByDescending(s => s.StartTime)
                .Take(5)
                .ToListAsync();

            if (recentSessions.Count > 0)
            {
                System.Console.WriteLine("\n   最近的会话:");
                foreach (var session in recentSessions)
                {
                    System.Console.WriteLine($"   - {session.DisplayName} ({session.ProcessName}): {session.DurationSeconds}s at {session.StartTime}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"   ❌ 数据库连接失败: {ex.Message}");
        }

        // 3. 测试WindowMonitor
        System.Console.WriteLine("\n3. 测试WindowMonitor...");
        try
        {
            var windowMonitor = new WindowMonitor();
            var currentWindow = windowMonitor.GetForegroundWindow();

            if (currentWindow != null)
            {
                System.Console.WriteLine($"   ✅ 当前前台窗口:");
                System.Console.WriteLine($"   - 进程名: {currentWindow.ProcessName}");
                System.Console.WriteLine($"   - 窗口标题: {currentWindow.WindowTitle}");
                System.Console.WriteLine($"   - 进程ID: {currentWindow.ProcessId}");
            }
            else
            {
                System.Console.WriteLine($"   ❌ 无法获取前台窗口");
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"   ❌ WindowMonitor测试失败: {ex.Message}");
        }

        // 4. 测试AppNameResolver
        System.Console.WriteLine("\n4. 测试AppNameResolver...");
        var testProcesses = new[] { "chrome", "Code", "WeChat", "explorer" };
        foreach (var process in testProcesses)
        {
            var friendlyName = AppNameResolver.GetFriendlyName(process);
            System.Console.WriteLine($"   {process} → {friendlyName}");
        }

        System.Console.WriteLine("\n=== 诊断完成 ===");
        System.Console.WriteLine("按任意键退出...");
        System.Console.ReadKey();
    }
}
