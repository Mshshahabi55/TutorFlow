using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorApplicationPhoneNumber : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                schema: "tutors",
                table: "TutorApplications",
                type: "character varying(32)",
                maxLength: 32,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                schema: "tutors",
                table: "TutorApplications");
        }
    }
}
