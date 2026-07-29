using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTutorProfileEnrichment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Biography",
                table: "Tutors",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Certifications",
                table: "Tutors",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Tutors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Country",
                table: "Tutors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DisplayName",
                table: "Tutors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Education",
                table: "Tutors",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "GalleryImageUrls",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Headline",
                table: "Tutors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IntroVideoUrl",
                table: "Tutors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LessonSpecialties",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OtherLanguages",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Tutors",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            // ADR-024's own Migration Strategy: existing rows default to
            // Submitted (not the new-registration default of Draft), so no
            // currently-approved Tutor is silently treated as an
            // incomplete onboarding draft by this change. A brand-new
            // Tutor.Register() call still starts at Draft in C# — this
            // default applies only to rows that already existed before
            // this migration ran.
            migrationBuilder.AddColumn<string>(
                name: "ProfileStatus",
                table: "Tutors",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Submitted");

            migrationBuilder.AddColumn<string>(
                name: "TeachingMethodology",
                table: "Tutors",
                type: "character varying(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TrialLessonAvailable",
                table: "Tutors",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<decimal>(
                name: "TrialLessonPrice",
                table: "Tutors",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TutorSubjects",
                table: "Tutors",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "YearsOfExperience",
                table: "Tutors",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Biography",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Certifications",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Country",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "DisplayName",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Education",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "GalleryImageUrls",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "Headline",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "IntroVideoUrl",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "LessonSpecialties",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "OtherLanguages",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "ProfileStatus",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TeachingMethodology",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TrialLessonAvailable",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TrialLessonPrice",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "TutorSubjects",
                table: "Tutors");

            migrationBuilder.DropColumn(
                name: "YearsOfExperience",
                table: "Tutors");
        }
    }
}
