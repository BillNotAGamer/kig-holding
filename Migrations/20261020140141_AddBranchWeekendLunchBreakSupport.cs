using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddBranchWeekendLunchBreakSupport : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LunchBreakAppliesOnWeekend",
                table: "Branches",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE \"Branches\" SET \"LunchBreakStart\" = '14:30:00', \"LunchBreakEnd\" = '16:30:00' WHERE \"LunchBreakStart\" IS NULL OR \"LunchBreakEnd\" IS NULL;");
            migrationBuilder.Sql("UPDATE \"Branches\" SET \"LunchBreakAppliesOnWeekend\" = TRUE WHERE \"Slug\" LIKE '%tan-phu%' OR \"Name\" ILIKE '%Tân Phú%';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LunchBreakAppliesOnWeekend",
                table: "Branches");
        }
    }
}
