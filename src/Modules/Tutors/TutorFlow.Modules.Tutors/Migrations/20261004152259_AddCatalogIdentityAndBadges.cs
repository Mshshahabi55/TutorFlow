using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <inheritdoc />
    public partial class AddCatalogIdentityAndBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "languages",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "specialties",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "subjects",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.AddColumn<string>(
                name: "CountryOfBirthCode",
                schema: "tutors",
                table: "TutorApplications",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfessionalTutor",
                schema: "tutors",
                table: "TutorApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperTutor",
                schema: "tutors",
                table: "TutorApplications",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "specialty_ids",
                schema: "tutors",
                table: "TutorApplications",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<string>(
                name: "spoken_languages",
                schema: "tutors",
                table: "TutorApplications",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "subject_ids",
                schema: "tutors",
                table: "TutorApplications",
                type: "uuid[]",
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CountryOfBirthCode",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "IsProfessionalTutor",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "IsSuperTutor",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "specialty_ids",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "spoken_languages",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.DropColumn(
                name: "subject_ids",
                schema: "tutors",
                table: "TutorApplications");

            migrationBuilder.AddColumn<List<string>>(
                name: "languages",
                schema: "tutors",
                table: "TutorApplications",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "specialties",
                schema: "tutors",
                table: "TutorApplications",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "subjects",
                schema: "tutors",
                table: "TutorApplications",
                type: "text[]",
                nullable: false);
        }
    }
}
