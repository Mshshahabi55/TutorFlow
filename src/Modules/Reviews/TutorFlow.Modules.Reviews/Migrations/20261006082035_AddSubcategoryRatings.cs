using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Reviews.Migrations
{
    /// <inheritdoc />
    public partial class AddSubcategoryRatings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SubcategoryRatings",
                schema: "reviews",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Clarity = table.Column<int>(type: "integer", nullable: true),
                    Progress = table.Column<int>(type: "integer", nullable: true),
                    Preparation = table.Column<int>(type: "integer", nullable: true),
                    Reassurance = table.Column<int>(type: "integer", nullable: true),
                    SubmissionMonthKey = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubcategoryRatings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SubcategoryRatings_StudentUserId_TutorUserId_SubmissionMonthKey",
                schema: "reviews",
                table: "SubcategoryRatings",
                columns: new[] { "StudentUserId", "TutorUserId", "SubmissionMonthKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubcategoryRatings_TutorUserId",
                schema: "reviews",
                table: "SubcategoryRatings",
                column: "TutorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SubcategoryRatings",
                schema: "reviews");
        }
    }
}
