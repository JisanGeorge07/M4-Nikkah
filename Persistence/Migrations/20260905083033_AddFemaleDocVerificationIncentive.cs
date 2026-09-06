using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFemaleDocVerificationIncentive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "female_doc_verification_amount",
                table: "staff_incentive_configs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "female_doc_verification_target",
                table: "staff_incentive_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "female_doc_verification_type",
                table: "staff_incentive_configs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "female_doc_verification_amount",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_doc_verification_target",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_doc_verification_type",
                table: "staff_incentive_configs");
        }
    }
}
