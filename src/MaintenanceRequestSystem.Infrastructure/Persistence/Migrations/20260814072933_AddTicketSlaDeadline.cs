using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketSlaDeadline : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "sla_due_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE tickets
                SET sla_due_at = created_at +
                    CASE priority
                        WHEN 'Critical' THEN INTERVAL '4 hours'
                        WHEN 'High' THEN INTERVAL '24 hours'
                        WHEN 'Medium' THEN INTERVAL '48 hours'
                        WHEN 'Low' THEN INTERVAL '72 hours'
                    END;
                """);

            migrationBuilder.AlterColumn<DateTime>(
                name: "sla_due_at",
                table: "tickets",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_sla_due_at",
                table: "tickets",
                column: "sla_due_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_tickets_sla_due_at",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "sla_due_at",
                table: "tickets");
        }
    }
}
