using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecordTime.Data.Migrations
{
    /// <inheritdoc />
    public partial class OptimizeDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Sessions_EndTime",
                table: "Sessions",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_LastHeartbeat",
                table: "Sessions",
                column: "LastHeartbeat");

            migrationBuilder.CreateIndex(
                name: "IX_Sessions_StartTime_EndTime",
                table: "Sessions",
                columns: new[] { "StartTime", "EndTime" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Sessions_EndTime",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_LastHeartbeat",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_Sessions_StartTime_EndTime",
                table: "Sessions");
        }
    }
}
