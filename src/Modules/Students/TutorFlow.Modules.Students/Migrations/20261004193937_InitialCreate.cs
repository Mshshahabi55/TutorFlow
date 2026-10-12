using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Students.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "students");

            migrationBuilder.CreateTable(
                name: "FavoriteTutors",
                schema: "students",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FavoriteTutors", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTutors_StudentUserId",
                schema: "students",
                table: "FavoriteTutors",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FavoriteTutors_StudentUserId_TutorUserId",
                schema: "students",
                table: "FavoriteTutors",
                columns: new[] { "StudentUserId", "TutorUserId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FavoriteTutors",
                schema: "students");
        }
    }
}
