using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Bookings.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingHoldExpiryIntent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BookingHoldExpiryIntents",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    IsTrial = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiredAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DispatchedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingHoldExpiryIntents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingHoldExpiryIntents_BookingId",
                schema: "bookings",
                table: "BookingHoldExpiryIntents",
                column: "BookingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BookingHoldExpiryIntents_Pending",
                schema: "bookings",
                table: "BookingHoldExpiryIntents",
                column: "DispatchedAtUtc",
                filter: "\"DispatchedAtUtc\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingHoldExpiryIntents",
                schema: "bookings");
        }
    }
}
