using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TutorFlow.Modules.Tutors.Migrations
{
    /// <summary>Data-only migration: `certificate_entries` is an EF owned-JSON column
    /// (`OwnsMany(...).ToJson()`), so the C# shape change (single `IssueYear` → `StartYear`/
    /// `EndYear`) has no relational DDL to apply — the `jsonb` column itself is unchanged. The one
    /// real thing that must happen is backfilling existing rows' JSON so `IssueYear: 2018` becomes
    /// `StartYear: 2018, EndYear: 2018` (confirmed against the actual dev-DB row for
    /// `International TEFL Academy` before writing this), since `CertificateEntry` no longer has
    /// an `IssueYear` property to read it back into. Keys are PascalCase, matching this
    /// application's existing (unconfigured, default) `System.Text.Json` serialization for owned
    /// JSON columns — confirmed by reading the real stored JSON, not assumed.</summary>
    public partial class AddCertificateEntryYearRange : Migration
    {
        private const string BackfillSql = """
            UPDATE tutors."TutorApplications"
            SET certificate_entries = (
                SELECT COALESCE(jsonb_agg(
                    CASE
                        WHEN elem ? 'IssueYear' THEN
                            (elem - 'IssueYear') || jsonb_build_object(
                                'StartYear', elem->'IssueYear',
                                'EndYear', elem->'IssueYear')
                        ELSE
                            elem
                    END
                ), '[]'::jsonb)
                FROM jsonb_array_elements(certificate_entries) AS elem
            )
            WHERE certificate_entries IS NOT NULL
              AND jsonb_typeof(certificate_entries) = 'array'
              AND EXISTS (
                  SELECT 1 FROM jsonb_array_elements(certificate_entries) AS e WHERE e ? 'IssueYear'
              );
            """;

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Per-element `WHEN elem ? 'IssueYear'` guard (not just the row-level WHERE EXISTS)
            // makes this idempotent even on a hypothetical mixed row: an element that has already
            // been migrated (no `IssueYear` key) is passed through untouched rather than having
            // its `StartYear`/`EndYear` recomputed from a now-absent key. Running this migration
            // twice is therefore safe and a no-op the second time.
            migrationBuilder.Sql(BackfillSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Intentionally not a full reverse: once a row has genuinely independent
            // StartYear/EndYear values (entered after this migration, not merely backfilled),
            // collapsing them back into a single IssueYear is inherently lossy — there is no
            // correct single year to pick. No data in this application has diverged yet at the
            // time this migration ships, but writing a silently-lossy Down() here would be worse
            // than being explicit that this direction isn't supported.
        }
    }
}
