using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddHistoricalReservationSoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DeletedAt",
                table: "Reservations",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "DeletedByAdminUserId",
                table: "Reservations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "Reservations",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "Version",
                table: "Reservations",
                type: "uuid",
                nullable: true);

            // A shared default revision would make existing reservations indistinguishable to
            // optimistic concurrency. Backfill each legacy row before making the column required.
            migrationBuilder.Sql(
                "UPDATE \"Reservations\" " +
                "SET \"Version\" = (md5(random()::text || clock_timestamp()::text || \"Id\"::text))::uuid " +
                "WHERE \"Version\" IS NULL;");

            migrationBuilder.AlterColumn<Guid>(
                name: "Version",
                table: "Reservations",
                type: "uuid",
                nullable: false,
                defaultValueSql: "gen_random_uuid()",
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_DeletedByAdminUserId",
                table: "Reservations",
                column: "DeletedByAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_IsDeleted_ReservationDate_ReservationTime",
                table: "Reservations",
                columns: new[] { "IsDeleted", "ReservationDate", "ReservationTime" });

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_AdminUsers_DeletedByAdminUserId",
                table: "Reservations",
                column: "DeletedByAdminUserId",
                principalTable: "AdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_AdminUsers_DeletedByAdminUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_DeletedByAdminUserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_IsDeleted_ReservationDate_ReservationTime",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "DeletedByAdminUserId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Reservations");
        }
    }
}
