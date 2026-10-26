using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <summary>Data-only migration, same reasoning as <c>AddCertificateEntryYearRange</c>:
    /// `certificate_entries` is still the same owned-JSON `jsonb` column — no relational DDL to
    /// apply. `VerificationStatus`/`RejectionReason` need no backfill: a JSON element missing
    /// either key deserializes to the new properties' correct defaults (`None`/`null`) via
    /// System.Text.Json's standard missing-member behavior, and there is no pre-existing data in
    /// either format to reconcile (confirmed: the one real certificate row in the dev DB has
    /// neither key at all). `Id` is the one property that genuinely needs backfilling — a missing
    /// `Id` key would otherwise deserialize every pre-Stage-2 entry to the same `Guid.Empty`,
    /// which is unusable as the stable identity every Stage 2 document/review endpoint now
    /// addresses a certificate by.</summary>
    public partial class AddCertificateVerificationAndStableId : Migration
    {
        private const string BackfillSql = """
            UPDATE tutors."TutorApplications"
            SET certificate_entries = (
                SELECT COALESCE(jsonb_agg(
                    CASE
                        WHEN elem ? 'Id' THEN elem
                        ELSE elem || jsonb_build_object('Id', gen_random_uuid())
                    END
                ), '[]'::jsonb)
                FROM jsonb_array_elements(certificate_entries) AS elem
            )
            WHERE certificate_entries IS NOT NULL
              AND jsonb_typeof(certificate_entries) = 'array'
              AND EXISTS (
                  SELECT 1 FROM jsonb_array_elements(certificate_entries) AS e WHERE NOT (e ? 'Id')
              );
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per-element guard (not just the row-level WHERE EXISTS) — idempotent for the same
            // reason as AddCertificateEntryYearRange: an element that already has an Id is passed
            // through unchanged, never re-randomized, on a second run.
            migrationBuilder.Sql(BackfillSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No reverse: the assigned Ids are now load-bearing identity for any certificate that
            // has since been addressed by a document-upload/review action — there is no correct
            // way to "un-assign" them, same reasoning as AddCertificateEntryYearRange's Down().
        }
    }
}
