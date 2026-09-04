using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Migrations
{
    /// <inheritdoc />
    public partial class AddStaffRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "concurrency_stamp", "name", "normalized_name" },
                values: new object[] { 2L, null, "Staff", "STAFF" });

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "concurrency_stamp", "password_hash" },
                values: new object[] { "6b35b46a-9c09-415d-a5ae-f68d60ae7ee8", "AQAAAAIAAYagAAAAEFQ8Ol8fZ5yeDHh6LfI6w7PjpTvI+O3eHokq4qEm17NUK239iMGxifhvSsOq8Sat9g==" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "roles",
                keyColumn: "id",
                keyValue: 2L);

            migrationBuilder.UpdateData(
                table: "users",
                keyColumn: "id",
                keyValue: 1L,
                columns: new[] { "concurrency_stamp", "password_hash" },
                values: new object[] { "ab5d6049-e401-4159-9c04-da30825ecef3", "AQAAAAIAAYagAAAAEOI0pa3uVXNPNDgo3DTBOugdjvX3gVenseja/DditneRbK7VPFdxmevvxD0jwBEgvA==" });
        }
    }
}
