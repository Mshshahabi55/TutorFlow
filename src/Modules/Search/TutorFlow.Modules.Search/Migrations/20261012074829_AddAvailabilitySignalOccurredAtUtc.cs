using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Search.Migrations
{
    /// <inheritdoc />
    public partial class AddAvailabilitySignalOccurredAtUtc : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "AvailabilitySignalOccurredAtUtc",
                schema: "search",
                table: "TutorSearchDocuments",
                type: "timestamp with time zone",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AvailabilitySignalOccurredAtUtc",
                schema: "search",
                table: "TutorSearchDocuments");
        }
    }
}
