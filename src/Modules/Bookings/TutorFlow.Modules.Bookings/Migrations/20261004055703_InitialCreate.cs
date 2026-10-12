using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Bookings.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "bookings");

            migrationBuilder.CreateTable(
                name: "Bookings",
                schema: "bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    StudentUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    TutorUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DurationMinutes = table.Column<int>(type: "integer", nullable: false),
                    StartUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    EndUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HeldAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    HoldExpiresAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ConfirmedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CancelledByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    PriceAmountMinorUnits = table.Column<int>(type: "integer", nullable: false),
                    PriceCurrency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    PaymentReferenceId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SubscriptionReferenceId = table.Column<Guid>(type: "uuid", nullable: true),
                    RescheduledFromBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    RescheduledToBookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_StudentUserId",
                schema: "bookings",
                table: "Bookings",
                column: "StudentUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_TutorUserId",
                schema: "bookings",
                table: "Bookings",
                column: "TutorUserId");

            // ADR-0013 §1 (corrected 2026-10-04 to span Held/PendingPayment/Confirmed, not just
            // Held/Confirmed — see the ADR for the double-booking gap this closes). EF Core has
            // no first-class mapping for PostgreSQL exclusion constraints or generated columns;
            // both are added here via raw SQL, an accepted, narrowly-scoped exception in an
            // otherwise EF-managed schema.
            migrationBuilder.Sql("CREATE EXTENSION IF NOT EXISTS btree_gist;");

            migrationBuilder.Sql("""
                ALTER TABLE bookings."Bookings"
                    ADD COLUMN "During" tstzrange GENERATED ALWAYS AS (tstzrange("StartUtc", "EndUtc", '[)')) STORED;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE bookings."Bookings"
                    ADD CONSTRAINT "EX_Bookings_NoOverlap"
                    EXCLUDE USING gist ("TutorUserId" WITH =, "During" WITH &&)
                    WHERE ("Status" IN ('Held', 'PendingPayment', 'Confirmed'));
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings",
                schema: "bookings");
        }
    }
}
