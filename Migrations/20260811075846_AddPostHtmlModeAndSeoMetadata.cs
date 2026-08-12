using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KIGHolding.Migrations
{
    /// <inheritdoc />
    public partial class AddPostHtmlModeAndSeoMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CanonicalUrl",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ContentMode",
                table: "Posts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FocusKeyword",
                table: "Posts",
                type: "character varying(120)",
                maxLength: 120,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgDescription",
                table: "Posts",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgImageUrl",
                table: "Posts",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OgTitle",
                table: "Posts",
                type: "character varying(180)",
                maxLength: 180,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RobotsFollow",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "RobotsIndex",
                table: "Posts",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CanonicalUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "ContentMode",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "FocusKeyword",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OgDescription",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OgImageUrl",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "OgTitle",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "RobotsFollow",
                table: "Posts");

            migrationBuilder.DropColumn(
                name: "RobotsIndex",
                table: "Posts");
        }
    }
}
