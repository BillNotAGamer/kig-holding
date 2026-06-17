using System;
using KIGHolding.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20260617210701_AddBranchLunchBreakHours")]
    public partial class AddBranchLunchBreakHours : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<TimeOnly>(
                name: "LunchBreakStart",
                table: "Branches",
                type: "time without time zone",
                nullable: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "LunchBreakEnd",
                table: "Branches",
                type: "time without time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LunchBreakStart",
                table: "Branches");

            migrationBuilder.DropColumn(
                name: "LunchBreakEnd",
                table: "Branches");
        }
    }
}
