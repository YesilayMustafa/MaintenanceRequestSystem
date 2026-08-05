using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "deleted_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "deleted_by_user_id",
                table: "tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_deleted",
                table: "tickets",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_is_deleted",
                table: "tickets",
                column: "is_deleted");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_is_deleted",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "deleted_at",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "deleted_by_user_id",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "is_deleted",
                table: "tickets");
        }
    }
}
