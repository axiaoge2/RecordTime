using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecordTime.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeBudgetTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoalSuggestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SuggestedType = table.Column<string>(type: "TEXT", nullable: false),
                    SuggestedMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Priority = table.Column<int>(type: "INTEGER", nullable: false),
                    BasedOnDays = table.Column<int>(type: "INTEGER", nullable: false),
                    HistoricalAverageMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IsProcessed = table.Column<bool>(type: "INTEGER", nullable: false),
                    ProcessResult = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoalSuggestions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TimeBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ProcessName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    DisplayName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Type = table.Column<string>(type: "TEXT", nullable: false),
                    TargetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ReminderEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    ReminderThreshold = table.Column<int>(type: "INTEGER", nullable: false),
                    IsEnabled = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeBudgets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DailyBudgetProgresses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimeBudgetId = table.Column<int>(type: "INTEGER", nullable: false),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetMinutes = table.Column<int>(type: "INTEGER", nullable: false),
                    ReminderSent = table.Column<bool>(type: "INTEGER", nullable: false),
                    GoalReached = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyBudgetProgresses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyBudgetProgresses_TimeBudgets_TimeBudgetId",
                        column: x => x.TimeBudgetId,
                        principalTable: "TimeBudgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DailyBudgetProgress_Date",
                table: "DailyBudgetProgresses",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_DailyBudgetProgress_TimeBudgetId",
                table: "DailyBudgetProgresses",
                column: "TimeBudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_DailyBudgetProgress_TimeBudgetId_Date",
                table: "DailyBudgetProgresses",
                columns: new[] { "TimeBudgetId", "Date" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GoalSuggestions_Category",
                table: "GoalSuggestions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_GoalSuggestions_ExpiresAt",
                table: "GoalSuggestions",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_GoalSuggestions_IsProcessed",
                table: "GoalSuggestions",
                column: "IsProcessed");

            migrationBuilder.CreateIndex(
                name: "IX_GoalSuggestions_ProcessName",
                table: "GoalSuggestions",
                column: "ProcessName");

            migrationBuilder.CreateIndex(
                name: "IX_TimeBudgets_Category",
                table: "TimeBudgets",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_TimeBudgets_IsEnabled",
                table: "TimeBudgets",
                column: "IsEnabled");

            migrationBuilder.CreateIndex(
                name: "IX_TimeBudgets_ProcessName",
                table: "TimeBudgets",
                column: "ProcessName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DailyBudgetProgresses");

            migrationBuilder.DropTable(
                name: "GoalSuggestions");

            migrationBuilder.DropTable(
                name: "TimeBudgets");
        }
    }
}
