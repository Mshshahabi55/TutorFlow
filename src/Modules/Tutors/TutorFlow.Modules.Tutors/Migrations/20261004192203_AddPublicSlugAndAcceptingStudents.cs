using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicSlugAndAcceptingStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAcceptingNewStudents",
                schema: "tutors",
                table: "TutorApplications",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "PublicSlug",
                schema: "tutors",
                table: "TutorApplications",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_TutorApplications_PublicSlug",
                schema: "tutors",
                table: "TutorApplications",
                column: "PublicSlug",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TutorApplications_PublicSlug",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "IsAcceptingNewStudents",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "PublicSlug",
                schema: "tutors",
                table: "TutorApplications");
        }
    }
}
