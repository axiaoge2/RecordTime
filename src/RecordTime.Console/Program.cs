namespace RecordTime.Console;

/// <summary>
/// RecordTime 命令行入口
/// </summary>
class Program
{
    public static async Task Main(string[] args)
    {
        System.Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
        {
            ShowHelp();
            return;
        }

        var command = args[0].ToLowerInvariant();
        var commandArgs = args.Skip(1).ToArray();

        try
        {
            switch (command)
            {
                case "monitor":
                case "m":
                    await ProgramEnhanced.Main(commandArgs);
                    break;

                case "report":
                case "r":
                    await GenerateReport.Main(commandArgs);
                    break;

                case "verify":
                case "v":
                    await VerifyData.Main(commandArgs);
                    break;

                case "diagnose":
                case "diag":
                case "d":
                    await DiagnosticTool.RunDiagnostics();
                    break;

                case "clear":
                case "c":
                    await ClearTodayData.Main(commandArgs);
                    break;

                case "help":
                case "h":
                case "--help":
                case "-h":
                    ShowHelp();
                    break;

                default:
                    System.Console.WriteLine($"❌ 未知命令: {command}");
                    System.Console.WriteLine();
                    ShowHelp();
                    break;
            }
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"❌ 执行失败: {ex.Message}");
            System.Console.WriteLine($"   {ex.StackTrace}");
        }
    }

    private static void ShowHelp()
    {
        System.Console.WriteLine("╔══════════════════════════════════════════════════╗");
        System.Console.WriteLine("║   RecordTime - Windows时间追踪工具 v2.0        ║");
        System.Console.WriteLine("╚══════════════════════════════════════════════════╝");
        System.Console.WriteLine();
        System.Console.WriteLine("用法: RecordTime <command> [options]");
        System.Console.WriteLine();
        System.Console.WriteLine("命令:");
        System.Console.WriteLine("  monitor, m     启动实时监控系统");
        System.Console.WriteLine("  report, r      生成HTML可视化报告");
        System.Console.WriteLine("  verify, v      查看数据库内容");
        System.Console.WriteLine("  clear, c       清空今天的数据");
        System.Console.WriteLine("  help, h        显示帮助信息");
        System.Console.WriteLine();
        System.Console.WriteLine("示例:");
        System.Console.WriteLine("  RecordTime monitor              # 启动监控");
        System.Console.WriteLine("  RecordTime report               # 生成今日报告");
        System.Console.WriteLine("  RecordTime report 2025-11-14    # 生成指定日期报告");
        System.Console.WriteLine("  RecordTime verify               # 查看今日数据");
        System.Console.WriteLine("  RecordTime clear                # 清空今日数据");
        System.Console.WriteLine();
        System.Console.WriteLine("更多信息: https://github.com/yourusername/recordtime");
    }
}
