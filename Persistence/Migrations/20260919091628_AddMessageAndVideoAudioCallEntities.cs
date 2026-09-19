using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMessageAndVideoAudioCallEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "call_logs",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    caller_id = table.Column<long>(type: "bigint", nullable: false),
                    receiver_id = table.Column<long>(type: "bigint", nullable: false),
                    call_type = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<int>(type: "int", nullable: false),
                    room_id = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    started_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    connected_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ended_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    duration_seconds = table.Column<int>(type: "int", nullable: false),
                    end_reason = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recording_s3key = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recording_url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    recording_file_size = table.Column<long>(type: "bigint", nullable: true),
                    recording_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_deleted_for_caller = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted_for_receiver = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("pk_call_logs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "conversations",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    type = table.Column<int>(type: "int", nullable: false),
                    user1id = table.Column<long>(type: "bigint", nullable: false),
                    user2id = table.Column<long>(type: "bigint", nullable: true),
                    assigned_staff_id = table.Column<long>(type: "bigint", nullable: true),
                    support_status = table.Column<int>(type: "int", nullable: false),
                    support_subject = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_message_id = table.Column<long>(type: "bigint", nullable: true),
                    last_message_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_message_preview = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_message_sender_id = table.Column<long>(type: "bigint", nullable: true),
                    is_blocked = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    blocked_by_user_id = table.Column<long>(type: "bigint", nullable: true),
                    user1unread_count = table.Column<int>(type: "int", nullable: false),
                    user2unread_count = table.Column<int>(type: "int", nullable: false),
                    support_unread_count = table.Column<int>(type: "int", nullable: false),
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
                    table.PrimaryKey("pk_conversations", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "chat_messages",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    conversation_id = table.Column<long>(type: "bigint", nullable: false),
                    sender_id = table.Column<long>(type: "bigint", nullable: false),
                    sender_role = table.Column<int>(type: "int", nullable: false),
                    receiver_id = table.Column<long>(type: "bigint", nullable: true),
                    content = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message_type = table.Column<int>(type: "int", nullable: false),
                    attachment_url = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attachment_file_name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    attachment_file_size = table.Column<long>(type: "bigint", nullable: true),
                    status = table.Column<int>(type: "int", nullable: false),
                    sent_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    delivered_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_deleted_for_sender = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    is_deleted_for_receiver = table.Column<bool>(type: "tinyint(1)", nullable: false),
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
                    table.PrimaryKey("pk_chat_messages", x => x.id);
                    table.ForeignKey(
                        name: "fk_chat_messages_conversations_conversation_id",
                        column: x => x.conversation_id,
                        principalTable: "conversations",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_caller_id_started_at",
                table: "call_logs",
                columns: new[] { "caller_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_call_logs_receiver_id_started_at",
                table: "call_logs",
                columns: new[] { "receiver_id", "started_at" });

            migrationBuilder.CreateIndex(
                name: "ix_chat_messages_conversation_id_sent_at",
                table: "chat_messages",
                columns: new[] { "conversation_id", "sent_at" });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_type_support_status",
                table: "conversations",
                columns: new[] { "type", "support_status" });

            migrationBuilder.CreateIndex(
                name: "ix_conversations_user1id_user2id",
                table: "conversations",
                columns: new[] { "user1id", "user2id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "call_logs");

            migrationBuilder.DropTable(
                name: "chat_messages");

            migrationBuilder.DropTable(
                name: "conversations");
        }
    }
}
