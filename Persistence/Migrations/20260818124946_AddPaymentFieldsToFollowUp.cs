using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPaymentFieldsToFollowUp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "offline_payment_type",
                table: "follow_ups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "payment_amount",
                table: "follow_ups",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "payment_completed",
                table: "follow_ups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "payment_link_sent",
                table: "follow_ups",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "payment_link_sent_at",
                table: "follow_ups",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payment_mode",
                table: "follow_ups",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "transaction_id",
                table: "follow_ups",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "offline_payment_type",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "payment_amount",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "payment_completed",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "payment_link_sent",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "payment_link_sent_at",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "payment_mode",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "transaction_id",
                table: "follow_ups");
        }
    }
}
