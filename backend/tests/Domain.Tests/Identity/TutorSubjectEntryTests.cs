using TutorFlow.Domain.Identity.ValueObjects;

namespace TutorFlow.Domain.Tests.Identity;

// Merge Readiness audit Critical 2: Subject/Level are free text and must be
// bounded the same as every other short Tutor field — enforced here, the
// single place per ADR-007's validation strategy, not duplicated in
// Application/Web.
public class TutorSubjectEntryTests
{
    [Fact]
    public void Of_trims_and_sets_subject_and_level()
    {
        var entry = TutorSubjectEntry.Of("  Mathematics  ", "  Beginner  ");

        Assert.Equal("Mathematics", entry.Subject);
        Assert.Equal("Beginner", entry.Level);
    }

    [Fact]
    public void Of_with_a_null_or_blank_level_sets_level_to_null()
    {
        var entry = TutorSubjectEntry.Of("Mathematics", "   ");

        Assert.Null(entry.Level);
    }

    [Fact]
    public void Of_with_a_null_subject_throws()
    {
        Assert.Throws<ArgumentException>(() => TutorSubjectEntry.Of(null!, null));
    }

    [Fact]
    public void Of_with_a_blank_subject_throws()
    {
        Assert.Throws<ArgumentException>(() => TutorSubjectEntry.Of("   ", null));
    }

    [Fact]
    public void Of_with_a_subject_exceeding_the_length_ceiling_throws()
    {
        Assert.Throws<ArgumentException>(() => TutorSubjectEntry.Of(new string('a', 201), null));
    }

    [Fact]
    public void Of_with_a_level_exceeding_the_length_ceiling_throws()
    {
        Assert.Throws<ArgumentException>(() => TutorSubjectEntry.Of("Mathematics", new string('a', 201)));
    }

    [Fact]
    public void Of_with_a_subject_at_exactly_the_length_ceiling_succeeds()
    {
        var entry = TutorSubjectEntry.Of(new string('a', 200), new string('b', 200));

        Assert.Equal(200, entry.Subject.Length);
        Assert.Equal(200, entry.Level!.Length);
    }
}
