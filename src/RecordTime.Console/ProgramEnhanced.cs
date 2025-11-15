using RecordTime.Core.Services;
using RecordTime.Core.Models;
using RecordTime.Data;
using RecordTime.Data.Repositories;
using Microsoft.EntityFrameworkCore;

namespace RecordTime.Console;

/// <summary>
/// 增强版测试程序 - 集成所有新功能
/// </summary>
class ProgramEnhanced
{
    public static async Task Main(string[] args)
    {
        System.Console.WriteLine("╔══════════════════════════════════════════════════╗");
        System.Console.WriteLine("║   RecordTime - 完整监控系统 v2.0               ║");
        System.Console.WriteLine("║   支持: 窗口监控 + 键鼠追踪 + 视频检测         ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════╝");
        System.Console.WriteLine();

        // 初始化数据库
        System.Console.WriteLine("📊 初始化数据库...");
        using (var dbContext = new RecordTimeDbContext())
        {
            await dbContext.Database.EnsureCreatedAsync();
        }
        System.Console.WriteLine("✅ 数据库就绪");

        // 创建所有监控服务
        System.Console.WriteLine("🔧 初始化监控服务...");
        var windowMonitor = new WindowMonitor();
        var inputMonitor = new InputMonitor();
        var mediaDetector = new MediaDetector();
        var activityDetector = new ActivityDetector();

        // 创建Repository工厂函数
        RecordTime.Core.Repositories.ISessionRepository CreateRepository()
        {
            var dbContext = new RecordTimeDbContext();
            return new SessionRepository(dbContext);
        }

        // 创建SessionManager
        var sessionManager = new SessionManager(
            windowMonitor,
            inputMonitor,
            mediaDetector,
            activityDetector,
            CreateRepository
        );

        // 订阅事件
        sessionManager.SessionStarted += (sender, session) =>
        {
            System.Console.WriteLine($"\n⏰ {DateTime.Now:HH:mm:ss}");
            System.Console.WriteLine($"📌 应用: {session.ProcessName}");
            System.Console.WriteLine($"🏷️  分类: {session.Category}");
            System.Console.WriteLine($"🎯 活动: {session.ActivityType}");
            System.Console.WriteLine($"💯 置信度: {session.Confidence}%");
            System.Console.WriteLine($"{'─',60}");
        };

        System.Console.WriteLine("✅ 所有服务就绪\n");

        // 显示实时监控信息
        var monitorTimer = new Timer(_ =>
        {
            try
            {
                var keyboardCount = inputMonitor.GetKeyboardActivityCount(30);
                var mouseClicks = inputMonitor.GetMouseClickCount(30);
                var idleTime = inputMonitor.GetIdleTimeSeconds();
                var isMediaPlaying = mediaDetector.IsMediaPlaying();

                System.Console.WriteLine($"\n📡 实时状态 ({DateTime.Now:HH:mm:ss})");
                System.Console.WriteLine($"   ⌨️  键盘活动 (30s): {keyboardCount}");
                System.Console.WriteLine($"   🖱️  鼠标点击 (30s): {mouseClicks}");
                System.Console.WriteLine($"   ⏱️  空闲时间: {idleTime}秒");
                System.Console.WriteLine($"   {(isMediaPlaying ? "▶️  媒体播放中" : "⏸️  无媒体播放")}");
                System.Console.WriteLine();
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ 监控错误: {ex.Message}");
            }
        }, null, TimeSpan.FromSeconds(15), TimeSpan.FromSeconds(15));

        // 定期显示统计
        var statsTimer = new Timer(async _ =>
        {
            try
            {
                using var dbContext = new RecordTimeDbContext();
                var repository = new SessionRepository(dbContext);

                var today = DateTime.Today;
                var sessions = await repository.GetSessionsByDateAsync(today);
                var totalSeconds = sessions.Sum(s => s.DurationSeconds);

                // 按分类统计
                var categoryStats = sessions
                    .GroupBy(s => s.Category ?? "未分类")
                    .Select(g => new
                    {
                        Category = g.Key,
                        Duration = TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds))
                    })
                    .OrderByDescending(x => x.Duration)
                    .Take(5);

                // 按活动类型统计
                var activityStats = sessions
                    .GroupBy(s => s.ActivityType)
                    .Select(g => new
                    {
                        Type = g.Key,
                        Duration = TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds))
                    })
                    .OrderByDescending(x => x.Duration);

                System.Console.WriteLine($"\n📊 今日统计摘要 ({DateTime.Now:HH:mm:ss})");
                System.Console.WriteLine($"{'═',60}");
                System.Console.WriteLine($"⏱️  总活动时长: {TimeSpan.FromSeconds(totalSeconds):hh\\:mm\\:ss}");
                System.Console.WriteLine($"📝 会话数量: {sessions.Count}");
                System.Console.WriteLine();

                System.Console.WriteLine("🏷️  分类TOP5:");
                foreach (var stat in categoryStats)
                {
                    System.Console.WriteLine($"   • {stat.Category,-15} {stat.Duration:hh\\:mm\\:ss}");
                }

                System.Console.WriteLine();
                System.Console.WriteLine("🎯 活动类型:");
                foreach (var stat in activityStats)
                {
                    var icon = stat.Type switch
                    {
                        ActivityType.Video => "▶️ ",
                        ActivityType.Gaming => "🎮",
                        ActivityType.ActiveTyping => "⌨️ ",
                        ActivityType.PassiveBrowsing => "👁️ ",
                        ActivityType.Idle => "💤",
                        _ => "❓"
                    };
                    System.Console.WriteLine($"   {icon} {stat.Type,-20} {stat.Duration:hh\\:mm\\:ss}");
                }

                System.Console.WriteLine($"{'═',60}\n");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"❌ 统计错误: {ex.Message}");
            }
        }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

        // 启动会话管理器
        System.Console.WriteLine("🚀 启动完整监控系统...");
        System.Console.WriteLine("💡 提示:");
        System.Console.WriteLine("   • 切换窗口查看自动记录");
        System.Console.WriteLine("   • 键盘鼠标活动会被实时追踪");
        System.Console.WriteLine("   • 视频播放会自动识别");
        System.Console.WriteLine("   • 每15秒显示实时状态");
        System.Console.WriteLine("   • 每60秒显示统计摘要");
        System.Console.WriteLine();
        System.Console.WriteLine("⚠️  按 Ctrl+C 停止监控");
        System.Console.WriteLine($"{'═',60}\n");

        sessionManager.Start();

        // 等待用户中断
        var tcs = new TaskCompletionSource();
        System.Console.CancelKeyPress += async (s, e) =>
        {
            e.Cancel = true;
            System.Console.WriteLine("\n\n⏹️  正在停止监控系统...");

            await sessionManager.StopAsync();
            monitorTimer.Dispose();
            statsTimer.Dispose();

            System.Console.WriteLine("✅ 已安全停止");
            tcs.SetResult();
        };

        await tcs.Task;
    }
}
