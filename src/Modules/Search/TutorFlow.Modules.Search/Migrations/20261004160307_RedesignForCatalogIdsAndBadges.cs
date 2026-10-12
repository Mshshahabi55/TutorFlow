using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Search.Migrations
{
    /// <inheritdoc />
    public partial class RedesignForCatalogIdsAndBadges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "specialty_names",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "subject_names",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.AddColumn<string>(
                name: "CountryOfBirthCode",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "character varying(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsProfessionalTutor",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSuperTutor",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "LessonCount",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "PublishedAtUtc",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<List<Guid>>(
                name: "specialty_ids",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.AddColumn<List<Guid>>(
                name: "subject_ids",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "uuid[]",
                nullable: false);

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_CountryOfBirthCode",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "CountryOfBirthCode");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_IsProfessionalTutor",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "IsProfessionalTutor");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_IsSuperTutor",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "IsSuperTutor");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_PublishedAtUtc",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_specialty_ids",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "specialty_ids")
                .Annotation("Npgsql:IndexMethod", "gin");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_subject_ids",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "subject_ids")
                .Annotation("Npgsql:IndexMethod", "gin");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TutorSearchDocuments_CountryOfBirthCode",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TutorSearchDocuments_IsProfessionalTutor",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TutorSearchDocuments_IsSuperTutor",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TutorSearchDocuments_PublishedAtUtc",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TutorSearchDocuments_specialty_ids",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropIndex(
                name: "IX_TutorSearchDocuments_subject_ids",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "CountryOfBirthCode",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "IsProfessionalTutor",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "IsSuperTutor",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "LessonCount",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "PublishedAtUtc",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "specialty_ids",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.DropColumn(
                name: "subject_ids",
                schema: "search",
                table: "TutorSearchDocuments");

            migrationBuilder.AddColumn<List<string>>(
                name: "specialty_names",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "text[]",
                nullable: false);

            migrationBuilder.AddColumn<List<string>>(
                name: "subject_names",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "text[]",
                nullable: false);
        }
    }
}
