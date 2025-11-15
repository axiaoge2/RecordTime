using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Services;

namespace RecordTime.Data.Migrations;

/// <summary>
/// 更新已有数据的 DisplayName 字段
/// </summary>
public static class DisplayNameMigration
{
    /// <summary>
    /// 执行迁移：将所有记录的 DisplayName 更新为友好名称
    /// </summary>
    public static async Task MigrateDisplayNamesAsync(RecordTimeDbContext dbContext)
    {
        // 获取所有 DisplayName 为空或等于 ProcessName 的记录
        var sessions = await dbContext.Sessions
            .Where(s => string.IsNullOrEmpty(s.DisplayName) || s.DisplayName == s.ProcessName)
            .ToListAsync();

        if (sessions.Count == 0)
        {
            Console.WriteLine("无需更新，所有记录的 DisplayName 都已正确设置");
            return;
        }

        Console.WriteLine($"找到 {sessions.Count} 条需要更新的记录");

        int updatedCount = 0;
        foreach (var session in sessions)
        {
            var friendlyName = AppNameResolver.GetFriendlyName(session.ProcessName);

            // 只有当友好名称与当前不同时才更新
            if (session.DisplayName != friendlyName)
            {
                session.DisplayName = friendlyName;
                updatedCount++;
            }
        }

        if (updatedCount > 0)
        {
            await dbContext.SaveChangesAsync();
            Console.WriteLine($"✅ 成功更新 {updatedCount} 条记录的 DisplayName");
        }
        else
        {
            Console.WriteLine("所有记录的 DisplayName 都已是最新的");
        }
    }
}
