using System.Runtime.CompilerServices;

// Scoped to TutorFlow.Host.Tests only, not TutorFlow.Modules.Availability.Tests (which tests
// exclusively through HTTP, per that project's existing convention) — needed because the
// TutorPublished/TutorUnpublished cross-module Wolverine pipeline tests (ADR-0024) call
// ReplaceWeeklyRules.Handler directly, the same real production call path self-service requests
// use, without going through an HTTP round-trip inside a cross-module integration test.
[assembly: InternalsVisibleTo("TutorFlow.Host.Tests")]
