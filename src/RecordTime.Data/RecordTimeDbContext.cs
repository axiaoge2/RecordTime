using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;

namespace RecordTime.Data;

/// <summary>
/// 数据库上下文
/// </summary>
public class RecordTimeDbContext : DbContext
{
    public DbSet<AppSession> Sessions { get; set; } = null!;

    public RecordTimeDbContext()
    {
        // 使用 Migrations 而不是 EnsureCreated
        // EnsureCreated() 无法处理 schema 变更,已弃用
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RecordTime",
            "recordtime.db"
        );

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        optionsBuilder.UseSqlite($"Data Source={dbPath}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSession>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.WindowTitleHash).HasMaxLength(64);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.ActivityType).HasConversion<string>();

            // Phase 1 Task 1.3: 优化索引
            // 单列索引
            entity.HasIndex(e => e.StartTime).HasDatabaseName("IX_Sessions_StartTime");
            entity.HasIndex(e => e.EndTime).HasDatabaseName("IX_Sessions_EndTime");
            entity.HasIndex(e => e.ProcessName).HasDatabaseName("IX_Sessions_ProcessName");
            entity.HasIndex(e => e.ActivityType).HasDatabaseName("IX_Sessions_ActivityType");
            entity.HasIndex(e => e.LastHeartbeat).HasDatabaseName("IX_Sessions_LastHeartbeat");

            // 复合索引 - 优化日期范围查询
            entity.HasIndex(e => new { e.StartTime, e.EndTime }).HasDatabaseName("IX_Sessions_StartTime_EndTime");
        });
    }
}
