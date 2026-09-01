using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedReservationDatePolicy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedReservationDates",
                columns: table => new
                {
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedReservationDates", x => x.Date);
                });

            migrationBuilder.InsertData(
                table: "BlockedReservationDates",
                column: "Date",
                values:
                [
                    new DateOnly(2026, 9, 2),
                    new DateOnly(2026, 9, 3),
                    new DateOnly(2027, 1, 1),
                    new DateOnly(2027, 2, 5),
                    new DateOnly(2027, 2, 8),
                    new DateOnly(2027, 2, 9),
                    new DateOnly(2027, 2, 10),
                    new DateOnly(2027, 2, 11),
                    new DateOnly(2027, 4, 16),
                    new DateOnly(2027, 4, 30),
                    new DateOnly(2027, 5, 3),
                    new DateOnly(2027, 9, 2),
                    new DateOnly(2027, 9, 3),
                    new DateOnly(2028, 1, 3),
                    new DateOnly(2028, 1, 25),
                    new DateOnly(2028, 1, 26),
                    new DateOnly(2028, 1, 27),
                    new DateOnly(2028, 1, 28),
                    new DateOnly(2028, 1, 31),
                    new DateOnly(2028, 4, 4),
                    new DateOnly(2028, 5, 1),
                    new DateOnly(2028, 5, 2),
                    new DateOnly(2028, 9, 4)
                ]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedReservationDates");
        }
    }
}
