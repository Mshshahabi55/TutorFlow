using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorApplicationConcurrencyToken : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                schema: "tutors",
                table: "TutorApplications",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                schema: "tutors",
                table: "TutorApplications");
        }
    }
}
