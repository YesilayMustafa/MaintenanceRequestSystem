using MaintenanceRequestSystem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20260812120000_AddTicketAttachments")]
public sealed class AddTicketAttachments : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "ticket_attachments",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                ticket_id = table.Column<Guid>(type: "uuid", nullable: false),
                uploaded_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                original_file_name = table.Column<string>(
                    type: "character varying(255)",
                    maxLength: 255,
                    nullable: false),
                storage_key = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                content_type = table.Column<string>(
                    type: "character varying(100)",
                    maxLength: 100,
                    nullable: false),
                size_bytes = table.Column<long>(type: "bigint", nullable: false),
                created_at = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ticket_attachments", x => x.id);
                table.ForeignKey(
                    name: "FK_ticket_attachments_tickets_ticket_id",
                    column: x => x.ticket_id,
                    principalTable: "tickets",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_ticket_attachments_users_uploaded_by_user_id",
                    column: x => x.uploaded_by_user_id,
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ticket_attachments_ticket_id_created_at",
            table: "ticket_attachments",
            columns: new[] { "ticket_id", "created_at" });

        migrationBuilder.CreateIndex(
            name: "IX_ticket_attachments_uploaded_by_user_id",
            table: "ticket_attachments",
            column: "uploaded_by_user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ticket_attachments");
    }
}
