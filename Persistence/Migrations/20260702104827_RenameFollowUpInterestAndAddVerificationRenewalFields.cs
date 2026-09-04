using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RenameFollowUpInterestAndAddVerificationRenewalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "latest_profile_verification_status",
                table: "follow_ups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "latest_renewal_interest_status",
                table: "follow_ups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "profile_verification_status",
                table: "follow_up_timelines",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "renewal_interest_status",
                table: "follow_up_timelines",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "latest_profile_verification_status",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "latest_renewal_interest_status",
                table: "follow_ups");

            migrationBuilder.DropColumn(
                name: "profile_verification_status",
                table: "follow_up_timelines");

            migrationBuilder.DropColumn(
                name: "renewal_interest_status",
                table: "follow_up_timelines");
        }
    }
}
