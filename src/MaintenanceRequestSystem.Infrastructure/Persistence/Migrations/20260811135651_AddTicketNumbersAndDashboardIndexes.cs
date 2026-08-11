using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MaintenanceRequestSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketNumbersAndDashboardIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ticket_number_sequences",
                columns: table => new
                {
                    year = table.Column<int>(type: "integer", nullable: false),
                    last_value = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ticket_number_sequences", x => x.year);
                });

            migrationBuilder.AddColumn<string>(
                name: "ticket_number",
                table: "tickets",
                type: "character varying(15)",
                maxLength: 15,
                nullable: true);

            migrationBuilder.Sql(
                """
                WITH numbered_tickets AS
                (
                    SELECT
                        id,
                        EXTRACT(YEAR FROM created_at)::integer AS ticket_year,
                        ROW_NUMBER() OVER
                        (
                            PARTITION BY EXTRACT(YEAR FROM created_at)::integer
                            ORDER BY created_at, id
                        ) AS sequence_value
                    FROM tickets
                )
                UPDATE tickets AS ticket
                SET ticket_number =
                    'REQ-' ||
                    LPAD(numbered.ticket_year::text, 4, '0') ||
                    '-' ||
                    LPAD(numbered.sequence_value::text, 6, '0')
                FROM numbered_tickets AS numbered
                WHERE ticket.id = numbered.id;

                INSERT INTO ticket_number_sequences (year, last_value)
                SELECT
                    EXTRACT(YEAR FROM created_at)::integer,
                    COUNT(*)::bigint
                FROM tickets
                GROUP BY EXTRACT(YEAR FROM created_at)::integer
                ON CONFLICT (year) DO UPDATE
                SET last_value = EXCLUDED.last_value;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ticket_number",
                table: "tickets",
                type: "character varying(15)",
                maxLength: 15,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(15)",
                oldMaxLength: 15,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_tickets_ticket_number",
                table: "tickets",
                column: "ticket_number",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ticket_number_sequences");

            migrationBuilder.DropIndex(
                name: "IX_tickets_ticket_number",
                table: "tickets");

            migrationBuilder.DropColumn(
                name: "ticket_number",
                table: "tickets");
        }
    }
}
