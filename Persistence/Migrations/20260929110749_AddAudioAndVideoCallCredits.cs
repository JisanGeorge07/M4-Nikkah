using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAudioAndVideoCallCredits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<short>(
                name: "audio_call_contacts_purchased",
                table: "plan_purchases",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "audio_call_contacts_used",
                table: "plan_purchases",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "video_call_minutes_purchased",
                table: "plan_purchases",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "video_call_minutes_used",
                table: "plan_purchases",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<bool>(
                name: "interest_card_shown",
                table: "conversations",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "audio_call_contacts",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    contact_user_id = table.Column<long>(type: "bigint", nullable: false),
                    plan_purchase_id = table.Column<long>(type: "bigint", nullable: false),
                    first_called_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
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
                    table.PrimaryKey("pk_audio_call_contacts", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_audio_call_contacts_user_id_contact_user_id_plan_purchase_id",
                table: "audio_call_contacts",
                columns: new[] { "user_id", "contact_user_id", "plan_purchase_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audio_call_contacts");

            migrationBuilder.DropColumn(
                name: "audio_call_contacts_purchased",
                table: "plan_purchases");

            migrationBuilder.DropColumn(
                name: "audio_call_contacts_used",
                table: "plan_purchases");

            migrationBuilder.DropColumn(
                name: "video_call_minutes_purchased",
                table: "plan_purchases");

            migrationBuilder.DropColumn(
                name: "video_call_minutes_used",
                table: "plan_purchases");

            migrationBuilder.DropColumn(
                name: "interest_card_shown",
                table: "conversations");
        }
    }
}
