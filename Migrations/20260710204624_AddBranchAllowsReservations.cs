using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchAllowsReservations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowsReservations",
                table: "Branches",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowsReservations",
                table: "Branches");
        }
    }
}
