using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchWeeklyGuestCapacityLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "WeekdayMinGuests",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WeekendMinGuests",
                table: "Branches",
                type: "integer",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Branches\" SET \"WeekdayMinGuests\" = 6, \"WeekendMinGuests\" = 10 WHERE \"City\" ILIKE '%Hồ Chí Minh%' OR \"City\" ILIKE '%HCM%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeekdayMinGuests",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "WeekendMinGuests",
                table: "Branches");
        }
    }
}
