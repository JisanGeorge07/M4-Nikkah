using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateStaffIncentiveAndTargetConfigs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_complaint_deduction_configs");

            migrationBuilder.AddColumn<DateTime>(
                name: "incentive_effective_date",
                table: "staff_salary_configs",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "per_day_salary",
                table: "staff_salary_configs",
                type: "decimal(65,30)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "staff_incentive_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    male_verification_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    male_verification_target = table.Column<int>(type: "int", nullable: true),
                    male_verification_amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    female_verification_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    female_verification_target = table.Column<int>(type: "int", nullable: true),
                    female_verification_amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    male_conversion_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    male_conversion_target = table.Column<int>(type: "int", nullable: true),
                    male_conversion_amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    female_conversion_type = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    female_conversion_target = table.Column<int>(type: "int", nullable: true),
                    female_conversion_amount = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_on = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    modified_by = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_incentive_configs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "staff_performance_target_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    staff_id = table.Column<long>(type: "bigint", nullable: false),
                    male_verification_monthly_target = table.Column<int>(type: "int", nullable: false),
                    male_verification_daily_target = table.Column<int>(type: "int", nullable: false),
                    female_verification_monthly_target = table.Column<int>(type: "int", nullable: false),
                    female_verification_daily_target = table.Column<int>(type: "int", nullable: false),
                    male_conversion_monthly_target = table.Column<int>(type: "int", nullable: false),
                    male_conversion_daily_target = table.Column<int>(type: "int", nullable: false),
                    female_conversion_monthly_target = table.Column<int>(type: "int", nullable: false),
                    female_conversion_daily_target = table.Column<int>(type: "int", nullable: false),
                    created_on = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_on = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    modified_by = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_performance_target_configs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "staff_incentive_configs");

            migrationBuilder.DropTable(
                name: "staff_performance_target_configs");

            migrationBuilder.DropColumn(
                name: "incentive_effective_date",
                table: "staff_salary_configs");

            migrationBuilder.DropColumn(
                name: "per_day_salary",
                table: "staff_salary_configs");

            migrationBuilder.CreateTable(
                name: "staff_complaint_deduction_configs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    created_by = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_on = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    deduction_mode = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fixed_amount = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    modified_by = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    modified_on = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    only_approved_complaints_affect_salary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    staff_id = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_staff_complaint_deduction_configs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
