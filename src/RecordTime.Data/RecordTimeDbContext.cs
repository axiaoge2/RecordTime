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

            // 索引
            entity.HasIndex(e => e.StartTime);
            entity.HasIndex(e => e.ProcessName);
            entity.HasIndex(e => e.ActivityType);
        });
    }
}
