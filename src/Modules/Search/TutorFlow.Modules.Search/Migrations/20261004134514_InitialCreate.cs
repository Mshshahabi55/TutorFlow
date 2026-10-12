using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Search.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "search");

            // Trusted extension — tutorflow_app can create it without superuser, same as
            // Bookings' btree_gist (docker/postgres/init/01-create-app-role.sh, ADR-0013 §1).
            // Required by the gin_trgm_ops index below (ADR-0007's full-text/trigram search).
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS pg_trgm;");

            migrationBuilder.CreateTable(
                name: "TutorSearchDocuments",
                schema: "search",
                columns: table => new
                {
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Headline = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    PhotoUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    subject_names = table.Column<List<string>>(type: "text[]", nullable: false),
                    specialty_names = table.Column<List<string>>(type: "text[]", nullable: false),
                    spoken_language_codes = table.Column<List<string>>(type: "text[]", nullable: false),
                    native_language_codes = table.Column<List<string>>(type: "text[]", nullable: false),
                    HourlyRateMinorUnits = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    AverageRating = table.Column<decimal>(type: "numeric", nullable: false),
                    ReviewCount = table.Column<int>(type: "integer", nullable: false),
                    HasUpcomingAvailability = table.Column<bool>(type: "boolean", nullable: false),
                    IsPublished = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorSearchDocuments", x => x.TutorUserId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_AverageRating",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "AverageRating");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_HourlyRateMinorUnits",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "HourlyRateMinorUnits");

            migrationBuilder.CreateIndex(
                name: "IX_TutorSearchDocuments_IsPublished",
                schema: "search",
                table: "TutorSearchDocuments",
                column: "IsPublished");

            migrationBuilder.CreateIndex(
                name: "ix_tutor_search_documents_name_headline_trgm",
                schema: "search",
                table: "TutorSearchDocuments",
                columns: new[] { "DisplayName", "Headline" })
                .Annotation("Npgsql:IndexMethod", "gin")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops", "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TutorSearchDocuments",
                schema: "search");
        }
    }
}
