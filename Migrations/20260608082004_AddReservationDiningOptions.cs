using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddReservationDiningOptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
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

            migrationBuilder.AddColumn<string>(
                name: "DiningOccasionCodes",
                table: "Reservations",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DiningOccasionOtherNote",
                table: "Reservations",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiningGroupCodes",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DiningGroupOtherNote",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DiningOccasionCodes",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DiningOccasionOtherNote",
                table: "Reservations");
        }
    }
}
