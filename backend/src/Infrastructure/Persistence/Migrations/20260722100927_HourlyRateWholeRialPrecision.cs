using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class HourlyRateWholeRialPrecision : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ADR-019: Rial has no minor unit. A bare ALTER COLUMN TYPE
            // numeric(12,0) would silently *round* any fractional value it
            // found — verified empirically that Postgres's numeric-to-
            // lower-scale cast rounds rather than truncates or errors
            // (docs/phases/PHASE-04-REPORT.md Section 3) — which is exactly
            // the "choose a rounding rule for the caller" this phase's own
            // rules forbid choosing silently. No environment this migration
            // has been run against has ever had a fractional HourlyRate
            // (confirmed against tutorflow_dev before writing this
            // migration: its only non-null row was already a whole
            // number), but rather than trust that silently, this fails
            // loudly instead of rounding if it ever finds one.
            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF EXISTS (
                        SELECT 1 FROM "Tutors"
                        WHERE "HourlyRate" IS NOT NULL AND "HourlyRate" != TRUNC("HourlyRate")
                    ) THEN
                        RAISE EXCEPTION 'Cannot migrate Tutors.HourlyRate to numeric(12,0): a fractional value exists. Resolve it manually (this migration deliberately does not choose a rounding rule) before re-running this migration.';
                    END IF;
                END $$;
                """);

            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRate",
                table: "Tutors",
                type: "numeric(12,0)",
                precision: 12,
                scale: 0,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(10,2)",
                oldPrecision: 10,
                oldScale: 2,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "HourlyRate",
                table: "Tutors",
                type: "numeric(10,2)",
                precision: 10,
                scale: 2,
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "numeric(12,0)",
                oldPrecision: 12,
                oldScale: 0,
                oldNullable: true);
        }
    }
}
