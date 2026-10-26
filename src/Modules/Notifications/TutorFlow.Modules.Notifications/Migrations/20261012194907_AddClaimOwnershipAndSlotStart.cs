using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Notifications.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimOwnershipAndSlotStart : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ADR-0029 Addendum A. Defaults for any pre-existing row (none expected in practice —
            // this feature has not shipped yet — but documented regardless): OwnedUntilUtc
            // defaults to DateTimeOffset.MinValue, which is always "stale," so any pre-existing
            // unresolved row becomes immediately eligible for reconciliation takeover on the very
            // next tick. OwnerToken defaults to Guid.Empty, which matches no worker's token and is
            // overwritten with a fresh token the moment that takeover occurs. A pre-existing
            // *resolved* row never consults either column again.
            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "OwnedUntilUtc",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.AddColumn<Guid>(
                name: "OwnerToken",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "StartUtc",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));

            migrationBuilder.CreateIndex(
                name: "IX_SentAbandonedCheckoutEmails_OwnedUntilUtc",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails",
                column: "OwnedUntilUtc",
                filter: "\"WasSent\" = false AND \"SkippedReason\" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SentAbandonedCheckoutEmails_OwnedUntilUtc",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails");

            migrationBuilder.DropColumn(
                name: "OwnedUntilUtc",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails");

            migrationBuilder.DropColumn(
                name: "OwnerToken",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails");

            migrationBuilder.DropColumn(
                name: "StartUtc",
                schema: "notifications",
                table: "SentAbandonedCheckoutEmails");
        }
    }
}
