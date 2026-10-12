using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TutorFlow.Modules.Subjects.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "subjects");

            migrationBuilder.CreateTable(
                name: "Languages",
                schema: "subjects",
                columns: table => new
                {
                    Code = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Languages", x => x.Code);
                });

            migrationBuilder.CreateTable(
                name: "Subjects",
                schema: "subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subjects", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialties",
                schema: "subjects",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialties_Subjects_SubjectId",
                        column: x => x.SubjectId,
                        principalSchema: "subjects",
                        principalTable: "Subjects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "subjects",
                table: "Languages",
                columns: new[] { "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { "de", "German", true, 4 },
                    { "en", "English", true, 1 },
                    { "es", "Spanish", true, 2 },
                    { "fr", "French", true, 3 },
                    { "it", "Italian", true, 6 },
                    { "pt", "Portuguese", true, 5 }
                });

            migrationBuilder.InsertData(
                schema: "subjects",
                table: "Subjects",
                columns: new[] { "Id", "Code", "DisplayName", "IsActive", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0001-000000000001"), "english", "English", true, 1 },
                    { new Guid("00000000-0000-0000-0001-000000000002"), "spanish", "Spanish", true, 2 },
                    { new Guid("00000000-0000-0000-0001-000000000003"), "french", "French", true, 3 },
                    { new Guid("00000000-0000-0000-0001-000000000004"), "german", "German", true, 4 },
                    { new Guid("00000000-0000-0000-0001-000000000005"), "mathematics", "Mathematics", true, 5 }
                });

            migrationBuilder.InsertData(
                schema: "subjects",
                table: "Specialties",
                columns: new[] { "Id", "Code", "DisplayName", "IsActive", "SortOrder", "SubjectId" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0002-000000000001"), "conversation", "Conversation practice", true, 1, new Guid("00000000-0000-0000-0001-000000000001") },
                    { new Guid("00000000-0000-0000-0002-000000000002"), "exam-prep", "Exam preparation", true, 2, new Guid("00000000-0000-0000-0001-000000000001") },
                    { new Guid("00000000-0000-0000-0002-000000000003"), "business", "Business English", true, 3, new Guid("00000000-0000-0000-0001-000000000001") },
                    { new Guid("00000000-0000-0000-0002-000000000004"), "kids-teens", "Kids & teens", true, 4, new Guid("00000000-0000-0000-0001-000000000001") },
                    { new Guid("00000000-0000-0000-0002-000000000005"), "conversation", "Conversation practice", true, 1, new Guid("00000000-0000-0000-0001-000000000002") },
                    { new Guid("00000000-0000-0000-0002-000000000006"), "exam-prep", "Exam preparation", true, 1, new Guid("00000000-0000-0000-0001-000000000005") }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_SubjectId",
                schema: "subjects",
                table: "Specialties",
                column: "SubjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialties_SubjectId_Code",
                schema: "subjects",
                table: "Specialties",
                columns: new[] { "SubjectId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_Code",
                schema: "subjects",
                table: "Subjects",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Languages",
                schema: "subjects");

            migrationBuilder.DropTable(
                name: "Specialties",
                schema: "subjects");

            migrationBuilder.DropTable(
                name: "Subjects",
                schema: "subjects");
        }
    }
}
