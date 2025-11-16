using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RecordTime.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLastHeartbeatToSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastHeartbeat",
                table: "Sessions",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastHeartbeat",
                table: "Sessions");
        }
    }
}
