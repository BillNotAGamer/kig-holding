using System;
using KIGHolding.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddAdminUserTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AdminUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Username = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Role = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false, defaultValue: "SuperAdmin"),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminUsers", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminUsers_Username",
                table: "AdminUsers",
                column: "Username",
                unique: true);

            var adminUser = new AdminUser
            {
                Id = Guid.Parse("2d7d0dd3-7d9d-4d9b-93d8-8e2d2b8ed9b1"),
                Username = "admin",
                Role = "SuperAdmin",
                IsActive = true,
                CreatedAt = new DateTimeOffset(2026, 5, 8, 0, 0, 0, TimeSpan.Zero),
                UpdatedAt = new DateTimeOffset(2026, 5, 8, 0, 0, 0, TimeSpan.Zero)
            };
            var hasher = new PasswordHasher<AdminUser>();
            var passwordHash = hasher.HashPassword(adminUser, "Password123!");

            migrationBuilder.InsertData(
                table: "AdminUsers",
                columns: new[] { "Id", "Username", "PasswordHash", "Role", "IsActive", "CreatedAt", "UpdatedAt" },
                values: new object[] { adminUser.Id, adminUser.Username, passwordHash, adminUser.Role, adminUser.IsActive, adminUser.CreatedAt, adminUser.UpdatedAt });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AdminUsers",
                keyColumn: "Id",
                keyValue: Guid.Parse("2d7d0dd3-7d9d-4d9b-93d8-8e2d2b8ed9b1"));

            migrationBuilder.DropTable(
                name: "AdminUsers");
        }
    }
}
