using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace TutorFlow.Host.Tests;

/// <summary>
/// A44 #103 — regression guard for an accepted, documented risk (see
/// <c>docs/OPEN_QUESTIONS.md</c>'s A44 entry and <c>Program.cs</c>'s own comment on the four
/// <c>PersistMessagesWithPostgresql</c> calls): <c>ProcessStripeWebhook</c>'s outgoing-envelope
/// targeting is correct today only because Payments is the *last*-registered Ancillary store —
/// Wolverine 6.45.0 resolves an un-enrolled <c>IMessageStore</c> via ordinary last-registration-
/// wins DI resolution, not by type. <c>ProcessStripeWebhookTopologyTests</c> already proves the
/// *consequence* end-to-end (genuinely separate containers; it would fail with a
/// <c>relation ... does not exist</c> error if this ever broke), but that failure mode is slow
/// (real Postgres containers) and its error message doesn't point back to the actual cause. This
/// test checks the one thing that actually matters — source order — directly, in milliseconds,
/// with no database and no Wolverine runtime involved, so a reordering regression is caught with
/// a clear message before anyone needs to debug a cryptic SQL error.
///
/// Deliberately a source-text check, not a runtime/reflection one: without <c>Enroll&lt;T&gt;()</c>
/// (reverted — see the A44 writeup for why), Wolverine's own registered stores carry no type-keyed
/// metadata to introspect at runtime; the literal order these four calls appear in
/// <c>Program.cs</c> is the actual, only fact that determines which store wins.
/// </summary>
public sealed class WolverineAncillaryStoreRegistrationOrderTests
{
    [Fact]
    public void Program_RegistersPaymentsAsTheLastAncillaryWolverineStore()
    {
        var programCsPath = ProgramCsPath();
        Assert.True(File.Exists(programCsPath), $"Expected to find Program.cs at '{programCsPath}' relative to this test file — update the relative path below if TutorFlow.Host or TutorFlow.Host.Tests ever moves.");

        var source = File.ReadAllText(programCsPath);

        // Every PersistMessagesWithPostgresql(...) call's second argument is the schema name
        // string literal — "messaging" (Main) plus one per Ancillary store. Matched in the order
        // they appear in the file, which is the only thing that determines DI's last-registered-
        // wins fallback for an un-enrolled IMessageStore.
        var schemaNames = Regex.Matches(source, @"PersistMessagesWithPostgresql\(\s*[^,]+,\s*""([a-z]+)""")
            .Select(m => m.Groups[1].Value)
            .ToList();

        var ancillarySchemas = schemaNames.Where(s => s != "messaging").ToList();

        Assert.Equal(["bookings", "availability", "tutors", "payments"], ancillarySchemas);

        Assert.True(
            ancillarySchemas.Count > 0 && ancillarySchemas[^1] == "payments",
            "Payments must remain the LAST-registered Ancillary Wolverine store in Program.cs. " +
            "ProcessStripeWebhook's outgoing-envelope targeting is correct today only because of " +
            "this registration order (Wolverine 6.45.0's un-enrolled IMessageStore resolves via " +
            "ordinary last-registration-wins DI, not by DbContext type — see docs/OPEN_QUESTIONS.md's " +
            "A44 entry for the full investigation and why Enroll<T>() was tried and reverted). If " +
            "this order needed to change, re-run ProcessStripeWebhookTopologyTests (genuinely " +
            "separate per-store containers) and re-evaluate this accepted risk before proceeding.");
    }

    private static string ProgramCsPath([CallerFilePath] string thisFilePath = "") =>
        Path.Combine(Path.GetDirectoryName(thisFilePath)!, "..", "..", "src", "TutorFlow.Host", "Program.cs");
}
