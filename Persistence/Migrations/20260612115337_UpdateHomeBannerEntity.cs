using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateHomeBannerEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "appstore_button_link",
                table: "home_banner",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "is_appstore_button_enabled",
                table: "home_banner",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_playstore_button_enabled",
                table: "home_banner",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "playstore_button_link",
                table: "home_banner",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "appstore_button_link",
                table: "home_banner");

            migrationBuilder.DropColumn(
                name: "is_appstore_button_enabled",
                table: "home_banner");

            migrationBuilder.DropColumn(
                name: "is_playstore_button_enabled",
                table: "home_banner");

            migrationBuilder.DropColumn(
                name: "playstore_button_link",
                table: "home_banner");
        }
    }
}
