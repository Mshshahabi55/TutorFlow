using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Search.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAcceptingNewStudents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // defaultValue: true, not false — matches TutorApplication.IsAcceptingNewStudents'
            // own default (every tutor accepts new students unless they opt out), so any row
            // that predates this column isn't retroactively (and incorrectly) shown as
            // not-accepting until its next TutorPublished/availability-mutation event resyncs it.
            migrationBuilder.AddColumn<bool>(
                name: "IsAcceptingNewStudents",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAcceptingNewStudents",
                schema: "search",
                table: "TutorSearchDocuments");
        }
    }
}
