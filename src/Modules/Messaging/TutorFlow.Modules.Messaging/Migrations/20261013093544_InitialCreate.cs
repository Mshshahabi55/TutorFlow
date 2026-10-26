using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace TutorFlow.Modules.Messaging.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "messaging");

            migrationBuilder.CreateTable(
                name: "Conversations",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastMessageAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    StudentLastReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    TutorLastReadAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Conversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Messages",
                schema: "messaging",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConversationId = table.Column<Guid>(type: "uuid", nullable: false),
                    SenderUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "character varying(5000)", maxLength: 5000, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SequenceNumber = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Messages", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_StudentUserId_TutorUserId",
                schema: "messaging",
                table: "Conversations",
                columns: new[] { "StudentUserId", "TutorUserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Conversations_TutorUserId",
                schema: "messaging",
                table: "Conversations",
                column: "TutorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Messages_ConversationId_SequenceNumber",
                schema: "messaging",
                table: "Messages",
                columns: new[] { "ConversationId", "SequenceNumber" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Conversations",
                schema: "messaging");

            migrationBuilder.DropTable(
                name: "Messages",
                schema: "messaging");
        }
    }
}
