using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "tutors");

            migrationBuilder.CreateTable(
                name: "TutorApplications",
                schema: "tutors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SubmittedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    PublishedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RejectionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    SuspensionReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    About = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ProfilePhotoReference = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    IntroVideoReference = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    languages = table.Column<List<string>>(type: "text[]", nullable: false),
                    subjects = table.Column<List<string>>(type: "text[]", nullable: false),
                    specialties = table.Column<List<string>>(type: "text[]", nullable: false),
                    YearsOfExperience = table.Column<int>(type: "integer", nullable: true),
                    ExperienceSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    HourlyRateAmountMinorUnits = table.Column<int>(type: "integer", nullable: true),
                    HourlyRateCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: true),
                    AvailabilitySettingsConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    BookingSettingsConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    certificate_entries = table.Column<string>(type: "jsonb", nullable: true),
                    education_entries = table.Column<string>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorApplications", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorApplications_UserId",
                schema: "tutors",
                table: "TutorApplications",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorApplications",
                schema: "tutors");
        }
    }
}
