using Microsoft.EntityFrameworkCore;
using RecordTime.Core.Models;

namespace RecordTime.Data;

/// <summary>
/// 数据库上下文
/// </summary>
public class RecordTimeDbContext : DbContext
{
    public DbSet<AppSession> Sessions { get; set; } = null!;

    // Phase 4: 时间预算相关表
    public DbSet<TimeBudget> TimeBudgets { get; set; } = null!;
    public DbSet<DailyBudgetProgress> DailyBudgetProgresses { get; set; } = null!;
    public DbSet<GoalSuggestion> GoalSuggestions { get; set; } = null!;

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

        // 使用 WAL 模式提高并发性能，避免数据库锁定问题
        // Pooling=false 确保连接在 Dispose 时立即关闭，避免资源泄漏
        optionsBuilder.UseSqlite($"Data Source={dbPath};Mode=ReadWriteCreate;Cache=Shared");
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

        // Phase 4: TimeBudget 配置
        modelBuilder.Entity<TimeBudget>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessName).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Type).HasConversion<string>();

            // 索引优化
            entity.HasIndex(e => e.ProcessName).HasDatabaseName("IX_TimeBudgets_ProcessName");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_TimeBudgets_Category");
            entity.HasIndex(e => e.IsEnabled).HasDatabaseName("IX_TimeBudgets_IsEnabled");
        });

        // Phase 4: DailyBudgetProgress 配置
        modelBuilder.Entity<DailyBudgetProgress>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.TimeBudget)
                  .WithMany()
                  .HasForeignKey(e => e.TimeBudgetId)
                  .OnDelete(DeleteBehavior.Cascade);

            // 索引优化 - 按日期和预算ID查询
            entity.HasIndex(e => e.Date).HasDatabaseName("IX_DailyBudgetProgress_Date");
            entity.HasIndex(e => e.TimeBudgetId).HasDatabaseName("IX_DailyBudgetProgress_TimeBudgetId");
            entity.HasIndex(e => new { e.TimeBudgetId, e.Date })
                  .IsUnique()
                  .HasDatabaseName("IX_DailyBudgetProgress_TimeBudgetId_Date");
        });

        // Phase 4: GoalSuggestion 配置
        modelBuilder.Entity<GoalSuggestion>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ProcessName).HasMaxLength(200);
            entity.Property(e => e.Category).HasMaxLength(50);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.SuggestedType).HasConversion<string>();
            entity.Property(e => e.Reason).HasMaxLength(1000);
            entity.Property(e => e.ProcessResult).HasMaxLength(20);

            // 索引优化
            entity.HasIndex(e => e.ProcessName).HasDatabaseName("IX_GoalSuggestions_ProcessName");
            entity.HasIndex(e => e.Category).HasDatabaseName("IX_GoalSuggestions_Category");
            entity.HasIndex(e => e.IsProcessed).HasDatabaseName("IX_GoalSuggestions_IsProcessed");
            entity.HasIndex(e => e.ExpiresAt).HasDatabaseName("IX_GoalSuggestions_ExpiresAt");
        });
    }
}
