using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFemaleGradeBasedVerificationIncentive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "female_grade_a_verification_amount",
                table: "staff_incentive_configs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "female_grade_a_verification_target",
                table: "staff_incentive_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "female_grade_a_verification_type",
                table: "staff_incentive_configs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "female_grade_b_verification_amount",
                table: "staff_incentive_configs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "female_grade_b_verification_target",
                table: "staff_incentive_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "female_grade_b_verification_type",
                table: "staff_incentive_configs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "female_grade_c_verification_amount",
                table: "staff_incentive_configs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "female_grade_c_verification_target",
                table: "staff_incentive_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "female_grade_c_verification_type",
                table: "staff_incentive_configs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "female_grade_d_verification_amount",
                table: "staff_incentive_configs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "female_grade_d_verification_target",
                table: "staff_incentive_configs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "female_grade_d_verification_type",
                table: "staff_incentive_configs",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "verification_grade",
                table: "follow_ups",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "female_grade_a_verification_amount",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_a_verification_target",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_a_verification_type",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_b_verification_amount",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_b_verification_target",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_b_verification_type",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_c_verification_amount",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_c_verification_target",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_c_verification_type",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_d_verification_amount",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_d_verification_target",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "female_grade_d_verification_type",
                table: "staff_incentive_configs");

            migrationBuilder.DropColumn(
                name: "verification_grade",
                table: "follow_ups");
        }
    }
}
