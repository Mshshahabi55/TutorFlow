using System.Runtime.CompilerServices;

// Scoped to TutorFlow.Host.Tests only, not TutorFlow.Modules.Tutors.Tests (which tests
// exclusively through HTTP, per that project's existing convention) — needed because
// PublishApplication.Handler has no HTTP endpoint yet (see its own doc comment) but still
// needs direct, cross-module, end-to-end test coverage of the Wolverine pipeline it triggers.
[assembly: InternalsVisibleTo("TutorFlow.Host.Tests")]
