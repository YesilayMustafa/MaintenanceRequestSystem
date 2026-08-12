using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketCategoriesAndAdvancedSearchSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_categories",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    normalized_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_categories", x => x.id);
                });

            migrationBuilder.Sql(
                """
                INSERT INTO ticket_categories
                    (id, name, normalized_name, description, is_active, created_at, updated_at)
                VALUES
                    ('10000000-0000-0000-0000-000000000001', 'Donanım', 'DONANIM', NULL, TRUE, CURRENT_TIMESTAMP, NULL),
                    ('10000000-0000-0000-0000-000000000002', 'Yazılım', 'YAZILIM', NULL, TRUE, CURRENT_TIMESTAMP, NULL),
                    ('10000000-0000-0000-0000-000000000003', 'Ağ', 'AĞ', NULL, TRUE, CURRENT_TIMESTAMP, NULL),
                    ('10000000-0000-0000-0000-000000000004', 'Yazıcı', 'YAZICI', NULL, TRUE, CURRENT_TIMESTAMP, NULL),
                    ('10000000-0000-0000-0000-000000000005', 'Hesap ve Erişim', 'HESAP VE ERİŞİM', NULL, TRUE, CURRENT_TIMESTAMP, NULL),
                    ('10000000-0000-0000-0000-000000000006', 'Diğer', 'DİĞER', NULL, TRUE, CURRENT_TIMESTAMP, NULL);
                """);

            migrationBuilder.AddColumn<Guid>(
                name: "category_id",
                table: "tickets",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE tickets
                SET category_id = '10000000-0000-0000-0000-000000000006'
                WHERE category_id IS NULL;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "category_id",
                table: "tickets",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_category_id",
                table: "tickets",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_ticket_categories_normalized_name",
                table: "ticket_categories",
                column: "normalized_name",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_tickets_ticket_categories_category_id",
                table: "tickets",
                column: "category_id",
                principalTable: "ticket_categories",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_tickets_ticket_categories_category_id",
                table: "tickets");

            migrationBuilder.DropTable(
                name: "ticket_categories");

            migrationBuilder.DropIndex(
                name: "IX_tickets_category_id",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "tickets");
        }
    }
}
