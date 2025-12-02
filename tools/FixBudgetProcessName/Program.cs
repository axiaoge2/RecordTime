using Microsoft.EntityFrameworkCore;
using RecordTime.Data;
using Serilog;

// 配置 Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateLogger();

Log.Information("=== 修复时间预算 ProcessName ===");

try
{
    await using var context = new RecordTimeDbContext();

    // 获取所有时间预算
    var budgets = await context.TimeBudgets.ToListAsync();

    foreach (var budget in budgets)
    {
        if (string.IsNullOrEmpty(budget.ProcessName))
            continue;

        Log.Information("\n检查预算: {DisplayName}", budget.DisplayName);
        Log.Information("  当前 ProcessName: {ProcessName}", budget.ProcessName);

        // 查找是否有会话使用这个 ProcessName
        var matchingSessions = await context.Sessions
            .AsNoTracking()
            .Where(s => s.ProcessName == budget.ProcessName)
            .Take(1)
            .ToListAsync();

        if (matchingSessions.Count > 0)
        {
            Log.Information("  ✓ ProcessName 匹配正常");
            continue;
        }

        // 如果没有匹配的会话，尝试查找使用 DisplayName 匹配的会话
        var sessionsByDisplayName = await context.Sessions
            .AsNoTracking()
            .Where(s => s.DisplayName == budget.ProcessName)
            .Select(s => s.ProcessName)
            .Distinct()
            .ToListAsync();

        if (sessionsByDisplayName.Count > 0)
        {
            var correctProcessName = sessionsByDisplayName[0];
            Log.Warning("  ⚠️  发现错误! DisplayName '{DisplayName}' 被存储为 ProcessName", budget.ProcessName);
            Log.Information("  正确的 ProcessName 应该是: {CorrectProcessName}", correctProcessName);

            // 修复
            budget.ProcessName = correctProcessName;
            budget.UpdatedAt = DateTime.Now;

            Log.Information("  ✓ 已修复: {OldValue} -> {NewValue}", budget.ProcessName, correctProcessName);
        }
        else
        {
            Log.Warning("  ⚠️  无法找到匹配的会话，可能是应用从未使用过");
        }
    }

    await context.SaveChangesAsync();
    Log.Information("\n修复完成!");
}
catch (Exception ex)
{
    Log.Error(ex, "修复时发生错误");
}
