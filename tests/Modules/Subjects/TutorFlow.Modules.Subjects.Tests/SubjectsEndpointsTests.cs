using System.Net.Http.Json;

namespace TutorFlow.Modules.Subjects.Tests;

public sealed class SubjectsEndpointsTests(SubjectsApiFixture fixture) : IClassFixture<SubjectsApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();

    private sealed record SubjectDto(Guid Id, string Code, string DisplayName);
    private sealed record SpecialtyDto(Guid Id, Guid SubjectId, string Code, string DisplayName);
    private sealed record LanguageDto(string Code, string DisplayName);

    [Fact]
    public async Task GetSubjects_ReturnsSeededSubjectsOrderedBySortOrder()
    {
        var subjects = await _client.GetFromJsonAsync<List<SubjectDto>>("/api/subjects", TestContext.Current.CancellationToken);

        Assert.NotNull(subjects);
        Assert.NotEmpty(subjects);
        Assert.Contains(subjects, s => s.Code == "english");
        Assert.Equal(subjects.OrderBy(s => s.Code == "english" ? 0 : 1).First().Code, subjects[0].Code);
    }

    [Fact]
    public async Task GetSpecialties_FilteredBySubjectId_ReturnsOnlyThatSubjectsSpecialties()
    {
        var subjects = await _client.GetFromJsonAsync<List<SubjectDto>>("/api/subjects", TestContext.Current.CancellationToken);
        var english = subjects!.Single(s => s.Code == "english");

        var specialties = await _client.GetFromJsonAsync<List<SpecialtyDto>>(
            $"/api/specialties?subjectId={english.Id}", TestContext.Current.CancellationToken);

        Assert.NotNull(specialties);
        Assert.NotEmpty(specialties);
        Assert.All(specialties, s => Assert.Equal(english.Id, s.SubjectId));
    }

    [Fact]
    public async Task GetSpecialties_WithoutSubjectId_ReturnsAllActiveSpecialties()
    {
        var specialties = await _client.GetFromJsonAsync<List<SpecialtyDto>>("/api/specialties", TestContext.Current.CancellationToken);

        Assert.NotNull(specialties);
        Assert.True(specialties.Count >= 2, "Expected specialties across more than one subject when unfiltered.");
    }

    [Fact]
    public async Task GetLanguages_ReturnsSeededLanguageReferenceList()
    {
        var languages = await _client.GetFromJsonAsync<List<LanguageDto>>("/api/languages", TestContext.Current.CancellationToken);

        Assert.NotNull(languages);
        Assert.Contains(languages, l => l.Code == "en");
    }
}
