using RecordTime.Core.Models;
using System.Text;

namespace RecordTime.Core.Services;

/// <summary>
/// HTML报告生成器
/// </summary>
public class ReportGenerator
{
    public string GenerateDailyReport(
        List<AppSession> sessions,
        DateTime date,
        string outputPath)
    {
        var html = new StringBuilder();

        // 计算统计数据
        var totalSeconds = sessions.Sum(s => s.DurationSeconds);
        var totalDuration = TimeSpan.FromSeconds(totalSeconds);

        var categoryStats = sessions
            .GroupBy(s => s.Category ?? "未分类")
            .Select(g => new
            {
                Category = g.Key,
                Duration = TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds)),
                Count = g.Count(),
                Percentage = (double)g.Sum(s => s.DurationSeconds) / totalSeconds * 100
            })
            .OrderByDescending(x => x.Duration)
            .ToList();

        var activityStats = sessions
            .GroupBy(s => s.ActivityType)
            .Select(g => new
            {
                Type = g.Key,
                Duration = TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds)),
                Count = g.Count(),
                Percentage = (double)g.Sum(s => s.DurationSeconds) / totalSeconds * 100
            })
            .OrderByDescending(x => x.Duration)
            .ToList();

        var topApps = sessions
            .GroupBy(s => s.ProcessName)
            .Select(g => new
            {
                Name = g.Key,
                Duration = TimeSpan.FromSeconds(g.Sum(s => s.DurationSeconds)),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Duration)
            .Take(10)
            .ToList();

        // 生成HTML
        html.AppendLine("<!DOCTYPE html>");
        html.AppendLine("<html lang=\"zh-CN\">");
        html.AppendLine("<head>");
        html.AppendLine("    <meta charset=\"UTF-8\">");
        html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        html.AppendLine($"    <title>RecordTime - {date:yyyy年MM月dd日} 日报</title>");
        html.AppendLine("    <style>");
        html.AppendLine(@"
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'PingFang SC', 'Microsoft YaHei', sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            padding: 20px;
            min-height: 100vh;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: rgba(255, 255, 255, 0.95);
            border-radius: 20px;
            padding: 40px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
        }
        h1 {
            color: #667eea;
            font-size: 2.5em;
            margin-bottom: 10px;
            font-weight: 700;
        }
        .date {
            color: #666;
            font-size: 1.2em;
            margin-bottom: 30px;
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-bottom: 40px;
        }
        .summary-card {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 15px;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.4);
        }
        .summary-card h3 {
            font-size: 0.9em;
            opacity: 0.9;
            margin-bottom: 10px;
        }
        .summary-card .value {
            font-size: 2em;
            font-weight: bold;
        }
        .section {
            margin-bottom: 40px;
        }
        .section h2 {
            color: #333;
            font-size: 1.8em;
            margin-bottom: 20px;
            padding-bottom: 10px;
            border-bottom: 3px solid #667eea;
        }
        .chart-container {
            margin-bottom: 30px;
        }
        .bar-chart {
            display: flex;
            flex-direction: column;
            gap: 15px;
        }
        .bar-item {
            display: flex;
            align-items: center;
            gap: 15px;
        }
        .bar-label {
            width: 150px;
            font-weight: 500;
            color: #333;
        }
        .bar-wrapper {
            flex: 1;
            position: relative;
            height: 30px;
            background: #f0f0f0;
            border-radius: 15px;
            overflow: hidden;
        }
        .bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
            border-radius: 15px;
            display: flex;
            align-items: center;
            padding: 0 15px;
            color: white;
            font-weight: bold;
            font-size: 0.9em;
            transition: width 0.5s ease;
        }
        .bar-value {
            width: 120px;
            text-align: right;
            color: #666;
            font-size: 0.9em;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            background: white;
            border-radius: 10px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.05);
        }
        th {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 15px;
            text-align: left;
            font-weight: 600;
        }
        td {
            padding: 12px 15px;
            border-bottom: 1px solid #f0f0f0;
            color: #333;
        }
        tr:hover {
            background: #f8f9ff;
        }
        .footer {
            margin-top: 40px;
            padding-top: 20px;
            border-top: 2px solid #f0f0f0;
            text-align: center;
            color: #999;
            font-size: 0.9em;
        }
        .emoji {
            font-size: 1.2em;
        }
        </style>");
        html.AppendLine("</head>");
        html.AppendLine("<body>");
        html.AppendLine("    <div class=\"container\">");

        // 标题
        html.AppendLine("        <h1>📊 RecordTime 日报</h1>");
        html.AppendLine($"        <div class=\"date\">{date:yyyy年MM月dd日 dddd}</div>");

        // 汇总卡片
        html.AppendLine("        <div class=\"summary\">");
        html.AppendLine("            <div class=\"summary-card\">");
        html.AppendLine("                <h3>总活动时长</h3>");
        html.AppendLine($"                <div class=\"value\">{totalDuration:hh}h {totalDuration:mm}m</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"summary-card\">");
        html.AppendLine("                <h3>会话数量</h3>");
        html.AppendLine($"                <div class=\"value\">{sessions.Count}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"summary-card\">");
        html.AppendLine("                <h3>应用类型</h3>");
        html.AppendLine($"                <div class=\"value\">{categoryStats.Count}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("            <div class=\"summary-card\">");
        html.AppendLine("                <h3>活动种类</h3>");
        html.AppendLine($"                <div class=\"value\">{activityStats.Count}</div>");
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // 分类统计
        html.AppendLine("        <div class=\"section\">");
        html.AppendLine("            <h2>🏷️ 应用分类统计</h2>");
        html.AppendLine("            <div class=\"bar-chart\">");
        foreach (var stat in categoryStats.Take(8))
        {
            html.AppendLine("                <div class=\"bar-item\">");
            html.AppendLine($"                    <div class=\"bar-label\">{stat.Category}</div>");
            html.AppendLine("                    <div class=\"bar-wrapper\">");
            html.AppendLine($"                        <div class=\"bar-fill\" style=\"width: {stat.Percentage:F1}%\">");
            html.AppendLine($"                            {stat.Percentage:F1}%");
            html.AppendLine("                        </div>");
            html.AppendLine("                    </div>");
            html.AppendLine($"                    <div class=\"bar-value\">{stat.Duration:hh\\:mm\\:ss}</div>");
            html.AppendLine("                </div>");
        }
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // 活动类型统计
        html.AppendLine("        <div class=\"section\">");
        html.AppendLine("            <h2>🎯 活动类型分布</h2>");
        html.AppendLine("            <div class=\"bar-chart\">");
        foreach (var stat in activityStats)
        {
            var emoji = stat.Type switch
            {
                ActivityType.Video => "▶️",
                ActivityType.Gaming => "🎮",
                ActivityType.ActiveTyping => "⌨️",
                ActivityType.PassiveBrowsing => "👁️",
                ActivityType.Idle => "💤",
                _ => "❓"
            };

            html.AppendLine("                <div class=\"bar-item\">");
            html.AppendLine($"                    <div class=\"bar-label\"><span class=\"emoji\">{emoji}</span> {stat.Type}</div>");
            html.AppendLine("                    <div class=\"bar-wrapper\">");
            html.AppendLine($"                        <div class=\"bar-fill\" style=\"width: {stat.Percentage:F1}%\">");
            html.AppendLine($"                            {stat.Percentage:F1}%");
            html.AppendLine("                        </div>");
            html.AppendLine("                    </div>");
            html.AppendLine($"                    <div class=\"bar-value\">{stat.Duration:hh\\:mm\\:ss}</div>");
            html.AppendLine("                </div>");
        }
        html.AppendLine("            </div>");
        html.AppendLine("        </div>");

        // TOP应用
        html.AppendLine("        <div class=\"section\">");
        html.AppendLine("            <h2>⭐ TOP 10 应用</h2>");
        html.AppendLine("            <table>");
        html.AppendLine("                <thead>");
        html.AppendLine("                    <tr>");
        html.AppendLine("                        <th>排名</th>");
        html.AppendLine("                        <th>应用名称</th>");
        html.AppendLine("                        <th>使用时长</th>");
        html.AppendLine("                        <th>使用次数</th>");
        html.AppendLine("                    </tr>");
        html.AppendLine("                </thead>");
        html.AppendLine("                <tbody>");

        int rank = 1;
        foreach (var app in topApps)
        {
            html.AppendLine("                    <tr>");
            html.AppendLine($"                        <td><strong>#{rank++}</strong></td>");
            html.AppendLine($"                        <td>{app.Name}</td>");
            html.AppendLine($"                        <td>{app.Duration:hh\\:mm\\:ss}</td>");
            html.AppendLine($"                        <td>{app.Count} 次</td>");
            html.AppendLine("                    </tr>");
        }

        html.AppendLine("                </tbody>");
        html.AppendLine("            </table>");
        html.AppendLine("        </div>");

        // 页脚
        html.AppendLine("        <div class=\"footer\">");
        html.AppendLine($"            <p>生成时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        html.AppendLine("            <p>🤖 Powered by RecordTime - Windows时间追踪工具</p>");
        html.AppendLine("        </div>");

        html.AppendLine("    </div>");
        html.AppendLine("</body>");
        html.AppendLine("</html>");

        // 保存文件
        var fullPath = Path.GetFullPath(outputPath);
        File.WriteAllText(fullPath, html.ToString(), Encoding.UTF8);

        return fullPath;
    }
}
