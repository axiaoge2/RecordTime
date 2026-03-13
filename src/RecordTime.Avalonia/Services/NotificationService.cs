using System;
using System.Linq;
using System.Runtime.InteropServices;
using RecordTime.Core.Models;
using Serilog;

namespace RecordTime.Avalonia.Services;

/// <summary>
/// 系统通知服务 - 用于显示桌面通知
/// </summary>
public class NotificationService : IDisposable
{
    private BudgetTrackingService? _trackingService;
    private DailySummaryService? _summaryService;

    /// <summary>
    /// 通知显示事件 (可用于UI显示通知)
    /// </summary>
    public event EventHandler<BudgetNotificationEventArgs>? NotificationRequested;

    /// <summary>
    /// 初始化通知服务并连接追踪服务
    /// </summary>
    public void Initialize(BudgetTrackingService trackingService)
    {
        _trackingService = trackingService;
        _trackingService.ReminderTriggered += OnReminderTriggered;

        // 启动日末总结服务
        _summaryService = DailySummaryService.Instance;
        _summaryService.DailySummaryGenerated += OnDailySummaryGenerated;
        _summaryService.Start();

        Log.Information("通知服务已初始化");
    }

    /// <summary>
    /// 当追踪服务触发提醒时
    /// </summary>
    private void OnReminderTriggered(object? sender, BudgetProgressItem progressItem)
    {
        try
        {
            var budget = progressItem.Budget;
            string title;
            string message;
            NotificationType notificationType;

            if (budget.Type == BudgetType.Maximum)
            {
                // 上限目标提醒
                if (progressItem.IsOverBudget)
                {
                    title = "时间预算已超出";
                    message = $"「{budget.DisplayName}」已超出预算上限！\n" +
                             $"目标: {FormatMinutes(budget.TargetMinutes)}\n" +
                             $"实际: {FormatMinutes(progressItem.TodayActualMinutes)}";
                    notificationType = NotificationType.Warning;
                }
                else
                {
                    title = "时间预算提醒";
                    message = $"「{budget.DisplayName}」已使用 {progressItem.ProgressPercentage:F0}%\n" +
                             $"剩余: {FormatMinutes(progressItem.RemainingMinutes)}";
                    notificationType = NotificationType.Info;
                }
            }
            else
            {
                // 下限目标提醒
                if (progressItem.IsGoalMet)
                {
                    title = "目标已达成";
                    message = $"「{budget.DisplayName}」目标已达成！\n" +
                             $"今日使用: {FormatMinutes(progressItem.TodayActualMinutes)}";
                    notificationType = NotificationType.Success;
                }
                else
                {
                    title = "目标进度提醒";
                    message = $"「{budget.DisplayName}」进度: {progressItem.ProgressPercentage:F0}%\n" +
                             $"还需: {FormatMinutes(progressItem.RemainingMinutes)}";
                    notificationType = NotificationType.Info;
                }
            }

            // 显示通知
            ShowNotification(title, message, notificationType);

            // 触发事件供UI处理
            NotificationRequested?.Invoke(this, new BudgetNotificationEventArgs
            {
                Title = title,
                Message = message,
                NotificationType = notificationType,
                BudgetItem = progressItem
            });

            Log.Information("预算通知已发送: {Title} - {Message}", title, message);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送预算通知失败");
        }
    }

    /// <summary>
    /// 当日末总结生成时
    /// </summary>
    private void OnDailySummaryGenerated(object? sender, DailySummaryData summary)
    {
        try
        {
            var title = "📊 今日使用总结";
            var message = $"总时长: {summary.TotalDurationText}\n" +
                         $"会话数: {summary.SessionCount} 个\n" +
                         $"应用数: {summary.AppCount} 个";

            // 添加 TOP 3 应用信息
            if (summary.TopApps.Count > 0)
            {
                message += "\n\n🏆 最常用应用:";
                for (int i = 0; i < Math.Min(3, summary.TopApps.Count); i++)
                {
                    var app = summary.TopApps[i];
                    message += $"\n{i + 1}. {app.AppName} ({app.DurationText})";
                }
            }

            // 添加预算完成情况
            if (summary.BudgetResults.Count > 0)
            {
                var metCount = summary.BudgetResults.Count(b => b.GoalMet);
                message += $"\n\n🎯 预算完成: {metCount}/{summary.BudgetResults.Count}";
            }

            ShowNotification(title, message, NotificationType.Info);
            Log.Information("日末总结通知已发送: 总时长 {TotalMinutes} 分钟", summary.TotalMinutes);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "发送日末总结通知失败");
        }
    }

    /// <summary>
    /// 显示系统通知
    /// </summary>
    public void ShowNotification(string title, string message, NotificationType type = NotificationType.Info)
    {
        try
        {
            // 使用Windows原生通知API
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                ShowWindowsNotification(title, message, type);
            }
            else
            {
                // 其他平台只记录日志
                Log.Information("[通知] {Title}: {Message}", title, message);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "显示系统通知失败");
        }
    }

    /// <summary>
    /// 显示Windows系统通知
    /// </summary>
    private void ShowWindowsNotification(string title, string message, NotificationType type)
    {
        try
        {
            // 使用PowerShell发送Toast通知 (简单但有效的方式)
            var iconPath = type switch
            {
                NotificationType.Success => "✅",
                NotificationType.Warning => "⚠️",
                NotificationType.Error => "❌",
                _ => "ℹ️"
            };

            var script = $@"
[Windows.UI.Notifications.ToastNotificationManager, Windows.UI.Notifications, ContentType = WindowsRuntime] | Out-Null
[Windows.Data.Xml.Dom.XmlDocument, Windows.Data.Xml.Dom.XmlDocument, ContentType = WindowsRuntime] | Out-Null

$template = @""
<toast>
    <visual>
        <binding template=""ToastText02"">
            <text id=""1"">{EscapeXml(title)}</text>
            <text id=""2"">{EscapeXml(message)}</text>
        </binding>
    </visual>
    <audio silent=""true""/>
</toast>
""@

$xml = New-Object Windows.Data.Xml.Dom.XmlDocument
$xml.LoadXml($template)
$toast = New-Object Windows.UI.Notifications.ToastNotification $xml
[Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier(""RecordTime"").Show($toast)
";

            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -NonInteractive -Command \"{script.Replace("\"", "\\\"")}\"",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            process?.WaitForExit(5000); // 最多等待5秒

            Log.Debug("Windows通知已发送: {Title}", title);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "发送Windows通知失败，使用备用方式");
            // 备用方式：只记录日志
            Log.Information("[通知-备用] {Title}: {Message}", title, message);
        }
    }

    /// <summary>
    /// 转义XML特殊字符
    /// </summary>
    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;")
            .Replace("\n", "&#10;");
    }

    /// <summary>
    /// 格式化分钟数为可读字符串
    /// </summary>
    private static string FormatMinutes(int minutes)
    {
        var hours = minutes / 60;
        var mins = minutes % 60;
        if (hours > 0 && mins > 0)
            return $"{hours}小时{mins}分钟";
        else if (hours > 0)
            return $"{hours}小时";
        else
            return $"{mins}分钟";
    }

    /// <summary>
    /// 发送测试通知
    /// </summary>
    public void SendTestNotification()
    {
        ShowNotification(
            "RecordTime 测试通知",
            "如果您看到这条消息，说明通知功能正常工作！",
            NotificationType.Info);
    }

    public void Dispose()
    {
        if (_trackingService != null)
        {
            _trackingService.ReminderTriggered -= OnReminderTriggered;
        }

        if (_summaryService != null)
        {
            _summaryService.DailySummaryGenerated -= OnDailySummaryGenerated;
            _summaryService.Stop();
        }
    }
}

/// <summary>
/// 通知类型
/// </summary>
public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error
}

/// <summary>
/// 预算通知事件参数
/// </summary>
public class BudgetNotificationEventArgs : EventArgs
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationType NotificationType { get; set; }
    public BudgetProgressItem? BudgetItem { get; set; }
}
