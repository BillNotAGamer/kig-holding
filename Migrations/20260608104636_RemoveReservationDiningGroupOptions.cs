using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class RemoveReservationDiningGroupOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiningGroupCodes",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DiningGroupOtherNote",
                table: "Reservations");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DiningGroupCodes",
                table: "Reservations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiningGroupOtherNote",
                table: "Reservations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }
    }
}
