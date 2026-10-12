using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Availability.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "availability");

            migrationBuilder.CreateTable(
                name: "BookingSettings",
                schema: "availability",
                columns: table => new
                {
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TimeZoneId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MinNoticeHours = table.Column<int>(type: "integer", nullable: false),
                    MaxBookingWindowDays = table.Column<int>(type: "integer", nullable: false),
                    DefaultSlotDurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingSettings", x => x.TutorUserId);
                });

            migrationBuilder.CreateTable(
                name: "TimeOffPeriods",
                schema: "availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Reason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeOffPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TutorBookabilityRecords",
                schema: "availability",
                columns: table => new
                {
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    IsBookable = table.Column<bool>(type: "boolean", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TutorBookabilityRecords", x => x.TutorUserId);
                });

            migrationBuilder.CreateTable(
                name: "WeeklyAvailabilityRules",
                schema: "availability",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DayOfWeek = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WeeklyAvailabilityRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TimeOffPeriods_TutorUserId",
                schema: "availability",
                table: "TimeOffPeriods",
                column: "TutorUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WeeklyAvailabilityRules_TutorUserId",
                schema: "availability",
                table: "WeeklyAvailabilityRules",
                column: "TutorUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingSettings",
                schema: "availability");

            migrationBuilder.DropTable(
                name: "TimeOffPeriods",
                schema: "availability");

            migrationBuilder.DropTable(
                name: "TutorBookabilityRecords",
                schema: "availability");

            migrationBuilder.DropTable(
                name: "WeeklyAvailabilityRules",
                schema: "availability");
        }
    }
}
