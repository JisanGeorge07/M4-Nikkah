using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffCreatedFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "staff_created",
                table: "registration",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "staff_id",
                table: "registration",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "staff_created",
                table: "registration");

            migrationBuilder.DropColumn(
                name: "staff_id",
                table: "registration");
        }
    }
}
