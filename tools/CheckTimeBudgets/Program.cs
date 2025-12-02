using Microsoft.EntityFrameworkCore;
using RecordTime.Data;
using Serilog;

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("=== 检查时间预算配置 ===");

try
{
    await using var context = new RecordTimeDbContext();

    Log.Information("\n📊 时间预算列表:");
    var budgets = await context.TimeBudgets.ToListAsync();

    if (budgets.Count == 0)
    {
        Log.Warning("⚠️  没有找到任何时间预算配置");
    }
    else
    {
        foreach (var budget in budgets)
        {
            Log.Information("\n预算 ID: {Id}", budget.Id);
            Log.Information("  显示名称: {DisplayName}", budget.DisplayName);
            Log.Information("  进程名称: {ProcessName}", budget.ProcessName ?? "(分类目标)");
            Log.Information("  分类: {Category}", budget.Category ?? "(应用目标)");
            Log.Information("  类型: {Type}", budget.Type == RecordTime.Core.Models.BudgetType.Maximum ? "上限" : "下限");
            Log.Information("  目标时长: {TargetMinutes} 分钟 ({Hours}h {Minutes}m)",
                budget.TargetMinutes,
                budget.TargetMinutes / 60,
                budget.TargetMinutes % 60);
            Log.Information("  是否启用: {IsEnabled}", budget.IsEnabled ? "是" : "否");
            Log.Information("  提醒: {ReminderEnabled} (阈值: {Threshold}%)",
                budget.ReminderEnabled ? "开启" : "关闭",
                budget.ReminderThreshold);
        }
    }

    Log.Information("\n\n📋 今日使用中的Chrome相关会话:");
    var today = DateTime.Today;
    var tomorrow = today.AddDays(1);

    var chromeSessions = await context.Sessions
        .Where(s => s.StartTime >= today && s.StartTime < tomorrow)
        .ToListAsync();  // 先加载到内存,再进行Contains判断

    chromeSessions = chromeSessions
        .Where(s => s.ProcessName.Contains("chrome", StringComparison.OrdinalIgnoreCase))
        .OrderBy(s => s.StartTime)
        .ToList();

    if (chromeSessions.Count == 0)
    {
        Log.Warning("⚠️  今日没有找到任何Chrome相关的会话");
    }
    else
    {
        Log.Information("找到 {Count} 个Chrome会话", chromeSessions.Count);

        int totalSeconds = 0;
        foreach (var session in chromeSessions.Take(5))  // 只显示前5个
        {
            Log.Information("  进程: {ProcessName}, 显示名称: {DisplayName}, 时长: {Duration}秒",
                session.ProcessName,
                session.DisplayName,
                session.DurationSeconds);
            totalSeconds += session.DurationSeconds;
        }

        if (chromeSessions.Count > 5)
        {
            Log.Information("  ... 还有 {More} 个会话未显示", chromeSessions.Count - 5);
            totalSeconds = chromeSessions.Sum(s => s.DurationSeconds);
        }

        Log.Information("\n总时长: {TotalSeconds} 秒 = {Minutes} 分钟", totalSeconds, totalSeconds / 60);
    }

    Log.Information("\n\n📈 今日预算进度:");
    var progresses = await context.DailyBudgetProgresses
        .Where(p => p.Date.Date == today)
        .ToListAsync();

    if (progresses.Count == 0)
    {
        Log.Warning("⚠️  今日没有预算进度记录");
    }
    else
    {
        foreach (var progress in progresses)
        {
            var budget = budgets.FirstOrDefault(b => b.Id == progress.TimeBudgetId);
            Log.Information("  预算: {DisplayName}", budget?.DisplayName ?? $"ID {progress.TimeBudgetId}");
            Log.Information("    实际使用: {ActualMinutes} 分钟 / 目标: {TargetMinutes} 分钟",
                progress.ActualMinutes,
                progress.TargetMinutes);
            Log.Information("    进度: {Progress:F1}%",
                progress.TargetMinutes > 0 ? (double)progress.ActualMinutes / progress.TargetMinutes * 100 : 0);
            Log.Information("    最后更新: {LastUpdated}", progress.LastUpdated);
        }
    }
}
catch (Exception ex)
{
    Log.Error(ex, "检查时发生错误");
}

Log.Information("\n检查完成!");
