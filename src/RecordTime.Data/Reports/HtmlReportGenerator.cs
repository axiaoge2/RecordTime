using RecordTime.Data;
using RecordTime.Core.Models.AI;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordTime.Data.Reports;

public class HtmlReportGenerator
{
    private readonly RecordTimeDbContext _dbContext;

    public HtmlReportGenerator(RecordTimeDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// 生成报告（不含 AI 分析）
    /// </summary>
    public async Task<string> GenerateReportAsync(DateTime startDate, DateTime endDate)
    {
        return await GenerateReportAsync(startDate, endDate, null);
    }

    /// <summary>
    /// 生成报告（可选包含 AI 分析结果）
    /// </summary>
    public async Task<string> GenerateReportAsync(DateTime startDate, DateTime endDate, AIAnalysisResult? aiResult)
    {
        // 确保日期范围正确
        var start = startDate.Date;
        var end = endDate.Date.AddDays(1);

        var sessions = await _dbContext.Sessions
            .AsNoTracking()
            .Where(s => s.StartTime >= start && s.StartTime < end)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            throw new InvalidOperationException("选定日期范围内没有数据");
        }

        // 统计数据
        var totalSeconds = sessions.Sum(s => s.DurationSeconds);
        var totalHours = totalSeconds / 3600.0;

        // 按应用分组
        var appStats = sessions
            .GroupBy(s => s.ProcessName)
            .Select(g => new
            {
                AppName = g.First().DisplayName ?? g.Key,
                ProcessName = g.Key,
                Category = g.First().Category ?? "未分类",
                TotalSeconds = g.Sum(s => s.DurationSeconds),
                SessionCount = g.Count(),
                Percentage = (double)g.Sum(s => s.DurationSeconds) / totalSeconds * 100
            })
            .OrderByDescending(x => x.TotalSeconds)
            .Take(10)
            .ToList();

        // 按分类分组
        var categoryStats = sessions
            .GroupBy(s => s.Category ?? "未分类")
            .Select(g => new
            {
                Category = g.Key,
                TotalSeconds = g.Sum(s => s.DurationSeconds),
                Percentage = (double)g.Sum(s => s.DurationSeconds) / totalSeconds * 100
            })
            .OrderByDescending(x => x.TotalSeconds)
            .ToList();

        // 生成 HTML
        var html = GenerateHtml(startDate, endDate, totalHours, sessions.Count, appStats, categoryStats, aiResult);

        // 保存文件
        var reportsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "reports");
        Directory.CreateDirectory(reportsFolder);

        var fileName = $"report_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}_{DateTime.Now:HHmmss}.html";
        var filePath = Path.Combine(reportsFolder, fileName);

        await File.WriteAllTextAsync(filePath, html, Encoding.UTF8);

        return filePath;
    }

    private string GenerateHtml(DateTime startDate, DateTime endDate, double totalHours, int sessionCount, dynamic appStats, dynamic categoryStats, AIAnalysisResult? aiResult)
    {
        var sb = new StringBuilder();

        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"zh-CN\">");
        sb.AppendLine("<head>");
        sb.AppendLine("    <meta charset=\"UTF-8\">");
        sb.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"    <title>RecordTime 报告 ({startDate:yyyy-MM-dd} 至 {endDate:yyyy-MM-dd})</title>");
        sb.AppendLine("    <script src=\"https://cdn.jsdelivr.net/npm/chart.js\"></script>");
        sb.AppendLine("    <style>");
        sb.AppendLine(@"
        :root {
            --rt-bg: #F5F5F7;
            --rt-surface: #FFFFFF;
            --rt-border: rgba(60,60,67,0.18);
            --rt-border-subtle: rgba(60,60,67,0.08);
            --rt-shadow: 0 25px 55px rgba(19,0,0,0.08), 0 1px 0 rgba(255,255,255,0.20);
            --rt-blue: #0A84FF;
            --rt-text-primary: #1D1D1F;
            --rt-text-secondary: #6E6E73;
        }
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'SF Pro Display','SF Pro Text',-apple-system,BlinkMacSystemFont,'Segoe UI','Microsoft YaHei',sans-serif;
            background: var(--rt-bg);
            color: var(--rt-text-primary);
            line-height: 1.5;
            letter-spacing: -0.25px;
            padding: 64px 24px 80px;
        }
        .container {
            max-width: 1040px;
            margin: 0 auto;
            background: var(--rt-surface);
            border-radius: 22px;
            padding: 48px;
            box-shadow: var(--rt-shadow);
            border: 1px solid var(--rt-border-subtle);
        }
        .hero {
            display: flex;
            flex-direction: column;
            gap: 8px;
            margin-bottom: 32px;
        }
        .eyebrow {
            text-transform: uppercase;
            font-size: 13px;
            letter-spacing: 0.3px;
            color: var(--rt-text-secondary);
        }
        h1 {
            font-size: 36px;
            letter-spacing: -0.8px;
            color: var(--rt-text-primary);
            font-weight: 600;
        }
        .date-range {
            font-size: 18px;
            color: var(--rt-text-secondary);
        }
        .summary {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px,1fr));
            gap: 24px;
            margin-bottom: 40px;
        }
        .summary-card {
            background: rgba(255,255,255,0.94);
            border-radius: 16px;
            padding: 24px;
            border: 1px solid var(--rt-border-subtle);
            box-shadow: 0 10px 25px rgba(19,0,0,0.06);
            transition: transform 180ms ease, box-shadow 180ms ease;
        }
        .summary-card:hover {
            transform: translateY(-2px);
            box-shadow: 0 16px 35px rgba(19,0,0,0.10);
        }
        .summary-card small {
            text-transform: uppercase;
            font-size: 13px;
            letter-spacing: 0.4px;
            color: var(--rt-text-secondary);
            display: block;
            margin-bottom: 8px;
        }
        .summary-card strong {
            display: block;
            font-size: 34px;
            letter-spacing: -0.6px;
            color: var(--rt-text-primary);
            font-weight: 600;
        }
        .charts {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(320px,1fr));
            gap: 24px;
            margin-bottom: 32px;
        }
        .chart-container {
            background: rgba(255,255,255,0.96);
            border-radius: 16px;
            padding: 28px;
            border: 1px solid var(--rt-border-subtle);
            box-shadow: 0 14px 30px rgba(19,0,0,0.07);
        }
        .chart-container h2 {
            font-size: 20px;
            letter-spacing: -0.4px;
            margin-bottom: 16px;
            color: var(--rt-text-primary);
            font-weight: 600;
        }
        table {
            width: 100%;
            border-spacing: 0;
            margin-top: 8px;
            border: 1px solid var(--rt-border-subtle);
            border-radius: 16px;
            overflow: hidden;
        }
        th, td {
            padding: 16px 20px;
            text-align: left;
            font-size: 15px;
        }
        thead th {
            background: rgba(10,132,255,0.08);
            color: var(--rt-blue);
            letter-spacing: -0.2px;
            font-weight: 600;
        }
        tbody tr + tr {
            border-top: 1px solid var(--rt-border-subtle);
        }
        tbody tr:hover {
            background: rgba(10,132,255,0.06);
        }
        .chip {
            display: inline-flex;
            align-items: center;
            padding: 2px 10px;
            border-radius: 999px;
            background: rgba(60,60,67,0.08);
            font-size: 13px;
            letter-spacing: -0.2px;
            color: var(--rt-text-primary);
        }
        .footer {
            margin-top: 36px;
            text-align: center;
            color: var(--rt-text-secondary);
            font-size: 14px;
            letter-spacing: -0.2px;
        }
        /* AI 分析部分样式 */
        .ai-section {
            margin-top: 48px;
            padding-top: 40px;
            border-top: 1px solid var(--rt-border);
        }
        .ai-header {
            display: flex;
            align-items: center;
            gap: 12px;
            margin-bottom: 24px;
        }
        .ai-header h2 {
            font-size: 24px;
            letter-spacing: -0.5px;
            font-weight: 600;
        }
        .ai-badge {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 4px 12px;
            border-radius: 12px;
            font-size: 12px;
            font-weight: 600;
            letter-spacing: 0.3px;
        }
        .ai-summary-card {
            background: linear-gradient(135deg, rgba(102,126,234,0.08) 0%, rgba(118,75,162,0.08) 100%);
            border-radius: 16px;
            padding: 24px;
            margin-bottom: 24px;
            border: 1px solid rgba(102,126,234,0.2);
        }
        .ai-summary-card p {
            font-size: 16px;
            line-height: 1.7;
            color: var(--rt-text-primary);
        }
        .score-display {
            display: flex;
            align-items: center;
            gap: 16px;
            margin-bottom: 32px;
        }
        .score-circle {
            width: 100px;
            height: 100px;
            border-radius: 50%;
            display: flex;
            flex-direction: column;
            align-items: center;
            justify-content: center;
            font-weight: 600;
            box-shadow: 0 8px 24px rgba(0,0,0,0.12);
        }
        .score-circle.high { background: linear-gradient(135deg, #32D74B 0%, #30B54A 100%); color: white; }
        .score-circle.medium { background: linear-gradient(135deg, #FFD60A 0%, #FFB800 100%); color: #1D1D1F; }
        .score-circle.low { background: linear-gradient(135deg, #FF453A 0%, #FF3B30 100%); color: white; }
        .score-number { font-size: 32px; line-height: 1; }
        .score-label { font-size: 11px; letter-spacing: 0.5px; opacity: 0.9; margin-top: 2px; }
        .score-desc { flex: 1; }
        .score-desc h3 { font-size: 18px; margin-bottom: 4px; }
        .score-desc p { color: var(--rt-text-secondary); font-size: 14px; }
        .ai-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 24px;
            margin-bottom: 24px;
        }
        .ai-card {
            background: var(--rt-surface);
            border-radius: 16px;
            padding: 24px;
            border: 1px solid var(--rt-border-subtle);
            box-shadow: 0 8px 20px rgba(19,0,0,0.05);
        }
        .ai-card h3 {
            font-size: 16px;
            font-weight: 600;
            margin-bottom: 16px;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .ai-card h3 .icon { font-size: 18px; }
        .ai-card ul {
            list-style: none;
            padding: 0;
        }
        .ai-card ul li {
            padding: 10px 0;
            border-bottom: 1px solid var(--rt-border-subtle);
            font-size: 14px;
            line-height: 1.6;
            color: var(--rt-text-primary);
        }
        .ai-card ul li:last-child { border-bottom: none; }
        .ai-card ul li::before {
            content: '•';
            color: var(--rt-blue);
            margin-right: 10px;
            font-weight: bold;
        }
        .recommendation-item {
            padding: 16px;
            background: rgba(10,132,255,0.04);
            border-radius: 12px;
            margin-bottom: 12px;
            border-left: 3px solid var(--rt-blue);
        }
        .recommendation-item:last-child { margin-bottom: 0; }
        .recommendation-item h4 {
            font-size: 15px;
            font-weight: 600;
            margin-bottom: 6px;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .recommendation-item p {
            font-size: 14px;
            color: var(--rt-text-secondary);
            line-height: 1.6;
        }
        .priority-badge {
            padding: 2px 8px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 600;
        }
        .priority-high { background: rgba(255,69,58,0.15); color: #FF453A; }
        .priority-medium { background: rgba(255,159,10,0.15); color: #FF9F0A; }
        .priority-low { background: rgba(50,215,75,0.15); color: #32D74B; }
        .ai-footer {
            text-align: center;
            padding: 16px;
            background: rgba(60,60,67,0.04);
            border-radius: 12px;
            margin-top: 24px;
        }
        .ai-footer p {
            font-size: 13px;
            color: var(--rt-text-secondary);
        }
        @media (max-width: 640px) {
            body { padding: 32px 16px; }
            .container { padding: 32px 24px; border-radius: 20px; }
            .summary { grid-template-columns: 1fr; }
            .chart-container { padding: 24px; }
            h1 { font-size: 28px; }
            .ai-grid { grid-template-columns: 1fr; }
            .score-display { flex-direction: column; text-align: center; }
        }
        ");
        sb.AppendLine("    </style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("    <div class=\"container\">");

        // Hero Section
        sb.AppendLine("        <section class=\"hero\">");
        sb.AppendLine("            <span class=\"eyebrow\">RecordTime · Report</span>");
        sb.AppendLine("            <h1>使用概览</h1>");
        sb.AppendLine($"            <p class=\"date-range\">{startDate:yyyy年MM月dd日} – {endDate:yyyy年MM月dd日}</p>");
        sb.AppendLine("        </section>");

        // Summary Cards
        sb.AppendLine("        <div class=\"summary\">");
        sb.AppendLine("            <div class=\"summary-card\">");
        sb.AppendLine("                <small>总使用时长</small>");
        sb.AppendLine($"                <strong>{totalHours:F1}h</strong>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"summary-card\">");
        sb.AppendLine("                <small>会话次数</small>");
        sb.AppendLine($"                <strong>{sessionCount}</strong>");
        sb.AppendLine("            </div>");
        sb.AppendLine("            <div class=\"summary-card\">");
        sb.AppendLine("                <small>使用应用</small>");
        sb.AppendLine($"                <strong>{appStats.Count}</strong>");
        sb.AppendLine("            </div>");
        sb.AppendLine("        </div>");

        // Charts
        sb.AppendLine("        <div class=\"charts\">");

        // App Stats Chart
        sb.AppendLine("            <div class=\"chart-container\">");
        sb.AppendLine("                <h2>应用使用统计</h2>");
        sb.AppendLine("                <canvas id=\"appChart\"></canvas>");
        sb.AppendLine("            </div>");

        // Category Stats Chart
        sb.AppendLine("            <div class=\"chart-container\">");
        sb.AppendLine("                <h2>分类统计</h2>");
        sb.AppendLine("                <canvas id=\"categoryChart\"></canvas>");
        sb.AppendLine("            </div>");

        sb.AppendLine("        </div>");

        // Detailed Table
        sb.AppendLine("        <h2 style=\"font-size: 20px; letter-spacing: -0.4px; margin: 40px 0 16px; font-weight: 600;\">详细应用列表</h2>");
        sb.AppendLine("        <table>");
        sb.AppendLine("            <thead>");
        sb.AppendLine("                <tr>");
        sb.AppendLine("                    <th>排名</th>");
        sb.AppendLine("                    <th>应用名称</th>");
        sb.AppendLine("                    <th>分类</th>");
        sb.AppendLine("                    <th>使用时长</th>");
        sb.AppendLine("                    <th>占比</th>");
        sb.AppendLine("                    <th>使用次数</th>");
        sb.AppendLine("                </tr>");
        sb.AppendLine("            </thead>");
        sb.AppendLine("            <tbody>");

        int rank = 1;
        foreach (var app in appStats)
        {
            var hours = app.TotalSeconds / 3600.0;
            sb.AppendLine("                <tr>");
            sb.AppendLine($"                    <td>#{rank++}</td>");
            sb.AppendLine($"                    <td>{app.AppName}</td>");
            sb.AppendLine($"                    <td><span class=\"chip\">{app.Category}</span></td>");
            sb.AppendLine($"                    <td>{hours:F2}h</td>");
            sb.AppendLine($"                    <td>{app.Percentage:F1}%</td>");
            sb.AppendLine($"                    <td>{app.SessionCount}</td>");
            sb.AppendLine("                </tr>");
        }

        sb.AppendLine("            </tbody>");
        sb.AppendLine("        </table>");

        // AI 分析结果部分
        if (aiResult != null && aiResult.IsSuccess)
        {
            AppendAIAnalysisSection(sb, aiResult);
        }

        // Footer
        sb.AppendLine("        <div class=\"footer\">");
        sb.AppendLine($"            <p>报告生成时间：{DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>");
        sb.AppendLine("            <p>© 2025 RecordTime - 时间追踪工具</p>");
        sb.AppendLine("        </div>");

        sb.AppendLine("    </div>");

        // Chart.js Scripts
        sb.AppendLine("    <script>");

        // App Chart Data
        sb.AppendLine("        const appLabels = [");
        foreach (var app in appStats)
        {
            sb.AppendLine($"            '{app.AppName}',");
        }
        sb.AppendLine("        ];");

        sb.AppendLine("        const appData = [");
        foreach (var app in appStats)
        {
            sb.AppendLine($"            {app.Percentage:F2},");
        }
        sb.AppendLine("        ];");

        // Category Chart Data
        sb.AppendLine("        const categoryLabels = [");
        foreach (var cat in categoryStats)
        {
            sb.AppendLine($"            '{cat.Category}',");
        }
        sb.AppendLine("        ];");

        sb.AppendLine("        const categoryData = [");
        foreach (var cat in categoryStats)
        {
            sb.AppendLine($"            {cat.Percentage:F2},");
        }
        sb.AppendLine("        ];");

        // Chart configurations
        sb.AppendLine(@"
        const palette = ['#0A84FF','#5AC8FA','#64D2FF','#32D74B','#FFD60A','#FF9F0A','#FF453A','#BF5AF2','#AC8E68','#8E8E93'];
        const baseFont = { family: 'SF Pro Text,-apple-system,BlinkMacSystemFont,Segoe UI,Microsoft YaHei', size: 13, lineHeight: 1.4 };
        const sharedPlugins = {
            legend: {
                position: 'bottom',
                labels: {
                    color: '#1D1D1F',
                    font: baseFont,
                    usePointStyle: true,
                    padding: 16,
                    boxWidth: 8,
                    boxHeight: 8
                }
            },
            tooltip: {
                backgroundColor: '#1D1D1F',
                titleColor: '#FFFFFF',
                bodyColor: '#FFFFFF',
                cornerRadius: 10,
                titleFont: baseFont,
                bodyFont: baseFont,
                padding: 14,
                callbacks: {
                    label: function(ctx) {
                        return ctx.label + ' · ' + ctx.parsed.toFixed(1) + '%';
                    }
                }
            }
        };

        // App Chart
        new Chart(document.getElementById('appChart'), {
            type: 'doughnut',
            data: {
                labels: appLabels,
                datasets: [{
                    data: appData,
                    backgroundColor: palette,
                    borderWidth: 0,
                    hoverOffset: 6
                }]
            },
            options: {
                cutout: '65%',
                animation: { duration: 200, easing: 'easeInOutQuad' },
                plugins: sharedPlugins,
                responsive: true,
                maintainAspectRatio: true
            }
        });

        // Category Chart
        new Chart(document.getElementById('categoryChart'), {
            type: 'bar',
            data: {
                labels: categoryLabels,
                datasets: [{
                    data: categoryData,
                    backgroundColor: '#0A84FF',
                    borderRadius: 12,
                    borderSkipped: false
                }]
            },
            options: {
                animation: { duration: 200, easing: 'easeInOutQuad' },
                plugins: {
                    legend: { display: false },
                    tooltip: sharedPlugins.tooltip
                },
                responsive: true,
                maintainAspectRatio: true,
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: { color: 'rgba(60,60,67,0.16)' },
                        ticks: { color: '#6E6E73', font: baseFont }
                    },
                    x: {
                        ticks: { color: '#6E6E73', font: baseFont },
                        grid: { display: false }
                    }
                }
            }
        });
        ");

        sb.AppendLine("    </script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");

        return sb.ToString();
    }

    /// <summary>
    /// 生成 AI 分析结果的 HTML 部分
    /// </summary>
    private void AppendAIAnalysisSection(StringBuilder sb, AIAnalysisResult aiResult)
    {
        sb.AppendLine("        <!-- AI 分析结果 -->");
        sb.AppendLine("        <section class=\"ai-section\">");

        // Header
        sb.AppendLine("            <div class=\"ai-header\">");
        sb.AppendLine("                <h2>AI 智能分析</h2>");
        sb.AppendLine($"                <span class=\"ai-badge\">{aiResult.ModelUsed}</span>");
        sb.AppendLine("            </div>");

        // Summary Card
        sb.AppendLine("            <div class=\"ai-summary-card\">");
        sb.AppendLine($"                <p>{EscapeHtml(aiResult.Summary)}</p>");
        sb.AppendLine("            </div>");

        // Productivity Score
        var scoreClass = aiResult.ProductivityScore >= 80 ? "high" : aiResult.ProductivityScore >= 60 ? "medium" : "low";
        var scoreDesc = aiResult.ProductivityScore >= 80 ? "效率优秀" : aiResult.ProductivityScore >= 60 ? "效率良好" : "有待提升";
        var scoreAdvice = aiResult.ProductivityScore >= 80
            ? "继续保持良好的工作习惯！"
            : aiResult.ProductivityScore >= 60
                ? "整体表现不错，还有提升空间。"
                : "建议关注下方改进建议，提升工作效率。";

        sb.AppendLine("            <div class=\"score-display\">");
        sb.AppendLine($"                <div class=\"score-circle {scoreClass}\">");
        sb.AppendLine($"                    <span class=\"score-number\">{aiResult.ProductivityScore}</span>");
        sb.AppendLine("                    <span class=\"score-label\">效率评分</span>");
        sb.AppendLine("                </div>");
        sb.AppendLine("                <div class=\"score-desc\">");
        sb.AppendLine($"                    <h3>{scoreDesc}</h3>");
        sb.AppendLine($"                    <p>{scoreAdvice}</p>");
        sb.AppendLine("                </div>");
        sb.AppendLine("            </div>");

        // Insights and Issues Grid
        sb.AppendLine("            <div class=\"ai-grid\">");

        // Insights Card
        if (aiResult.Insights.Count > 0)
        {
            sb.AppendLine("                <div class=\"ai-card\">");
            sb.AppendLine("                    <h3><span class=\"icon\">💡</span>关键洞察</h3>");
            sb.AppendLine("                    <ul>");
            foreach (var insight in aiResult.Insights)
            {
                sb.AppendLine($"                        <li>{EscapeHtml(insight)}</li>");
            }
            sb.AppendLine("                    </ul>");
            sb.AppendLine("                </div>");
        }

        // Issues Card
        if (aiResult.Issues.Count > 0)
        {
            sb.AppendLine("                <div class=\"ai-card\">");
            sb.AppendLine("                    <h3><span class=\"icon\">⚠️</span>发现的问题</h3>");
            sb.AppendLine("                    <ul>");
            foreach (var issue in aiResult.Issues)
            {
                sb.AppendLine($"                        <li>{EscapeHtml(issue)}</li>");
            }
            sb.AppendLine("                    </ul>");
            sb.AppendLine("                </div>");
        }

        sb.AppendLine("            </div>");

        // Recommendations
        if (aiResult.Recommendations.Count > 0)
        {
            sb.AppendLine("            <div class=\"ai-card\" style=\"margin-top: 24px;\">");
            sb.AppendLine("                <h3><span class=\"icon\">📋</span>改进建议</h3>");

            foreach (var rec in aiResult.Recommendations)
            {
                var priorityClass = rec.Priority == AIPriority.High ? "priority-high"
                    : rec.Priority == AIPriority.Low ? "priority-low"
                    : "priority-medium";
                var priorityText = rec.Priority == AIPriority.High ? "高优先级"
                    : rec.Priority == AIPriority.Low ? "低优先级"
                    : "中优先级";

                sb.AppendLine("                <div class=\"recommendation-item\">");
                sb.AppendLine("                    <h4>");
                sb.AppendLine($"                        {EscapeHtml(rec.Title)}");
                sb.AppendLine($"                        <span class=\"priority-badge {priorityClass}\">{priorityText}</span>");
                sb.AppendLine("                    </h4>");
                sb.AppendLine($"                    <p>{EscapeHtml(rec.Description)}</p>");
                sb.AppendLine("                </div>");
            }

            sb.AppendLine("            </div>");
        }

        // AI Footer
        sb.AppendLine("            <div class=\"ai-footer\">");
        sb.AppendLine($"                <p>由 {aiResult.ModelUsed} 生成于 {aiResult.AnalyzedAt:yyyy-MM-dd HH:mm} · 分析结果仅供参考</p>");
        sb.AppendLine("            </div>");

        sb.AppendLine("        </section>");
    }

    /// <summary>
    /// HTML 转义
    /// </summary>
    private string EscapeHtml(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
