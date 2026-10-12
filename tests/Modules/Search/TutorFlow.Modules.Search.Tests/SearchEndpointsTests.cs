using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using TutorFlow.Modules.Search.Domain;
using TutorFlow.Modules.Search.Persistence;

namespace TutorFlow.Modules.Search.Tests;

public sealed class SearchEndpointsTests(SearchApiFixture fixture) : IClassFixture<SearchApiFixture>
{
    private readonly HttpClient _client = fixture.CreateClient();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private async Task SeedAsync(params TutorSearchDocument[] documents)
    {
        using var scope = fixture.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SearchDbContext>();
        db.TutorSearchDocuments.AddRange(documents);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken);
    }

    private static TutorSearchDocument MakeDocument(
        string displayName,
        Guid? subjectId = null,
        List<Guid>? specialtyIds = null,
        bool isPublished = true,
        int hourlyRateMinorUnits = 2000,
        decimal averageRating = 4.5m,
        List<string>? spokenLanguageCodes = null,
        List<string>? nativeLanguageCodes = null,
        string? countryOfBirthCode = null,
        bool isProfessionalTutor = false,
        bool isSuperTutor = false,
        bool hasUpcomingAvailability = true,
        bool isAcceptingNewStudents = true,
        DateTimeOffset? publishedAtUtc = null) =>
        new()
        {
            TutorUserId = Guid.NewGuid(),
            DisplayName = displayName,
            Headline = $"{displayName}'s headline",
            SubjectIds = subjectId is { } id ? [id] : [],
            SpecialtyIds = specialtyIds ?? [],
            SpokenLanguageCodes = spokenLanguageCodes ?? ["en"],
            NativeLanguageCodes = nativeLanguageCodes ?? ["en"],
            CountryOfBirthCode = countryOfBirthCode,
            IsProfessionalTutor = isProfessionalTutor,
            IsSuperTutor = isSuperTutor,
            HourlyRateMinorUnits = hourlyRateMinorUnits,
            Currency = "EUR",
            AverageRating = averageRating,
            ReviewCount = 10,
            HasUpcomingAvailability = hasUpcomingAvailability,
            IsAcceptingNewStudents = isAcceptingNewStudents,
            IsPublished = isPublished,
            PublishedAtUtc = publishedAtUtc ?? DateTimeOffset.UtcNow,
        };

    private sealed record ResponseDto(List<ItemDto> Items, int Page, int PageSize, int TotalCount);

    private sealed record ItemDto(
        Guid TutorUserId,
        string DisplayName,
        int HourlyRateMinorUnits,
        decimal AverageRating,
        bool IsAcceptingNewStudents);

    private async Task<ResponseDto?> GetAsync(string query)
    {
        var response = await _client.GetAsync(query, TestContext.Current.CancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ResponseDto>(JsonOptions, TestContext.Current.CancellationToken);
    }

    [Fact]
    public async Task SearchTutors_ExcludesUnpublishedTutors()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Published Tutor", subjectId, isPublished: true),
            MakeDocument("Draft Tutor", subjectId, isPublished: false));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Published Tutor", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersBySubjectId()
    {
        var math = Guid.NewGuid();
        var spanish = Guid.NewGuid();
        await SeedAsync(MakeDocument("Math Tutor", math), MakeDocument("Spanish Tutor", spanish));

        var response = await GetAsync($"/api/search/tutors?subjectId={math}");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Math Tutor", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_SpecialtyIds_UseAndSemantics_RequiresAllRequested()
    {
        var subjectId = Guid.NewGuid();
        var examPrep = Guid.NewGuid();
        var business = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Both", subjectId, specialtyIds: [examPrep, business]),
            MakeDocument("ExamPrepOnly", subjectId, specialtyIds: [examPrep]));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&specialtyIds={examPrep},{business}");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Both", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersByPriceRange()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Cheap Tutor", subjectId, hourlyRateMinorUnits: 1000),
            MakeDocument("Mid Tutor", subjectId, hourlyRateMinorUnits: 2500),
            MakeDocument("Expensive Tutor", subjectId, hourlyRateMinorUnits: 9000));

        var response = await GetAsync(
            $"/api/search/tutors?subjectId={subjectId}&priceMinMinorUnits=2000&priceMaxMinorUnits=5000");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Mid Tutor", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersByMinRating()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("High Rated", subjectId, averageRating: 4.9m),
            MakeDocument("Low Rated", subjectId, averageRating: 3.0m));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&minRating=4.5");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("High Rated", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersByNativeSpeaker()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Native Speaker", subjectId, spokenLanguageCodes: ["fr"], nativeLanguageCodes: ["fr"]),
            MakeDocument("Fluent Only", subjectId, spokenLanguageCodes: ["fr"], nativeLanguageCodes: []));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&nativeSpeakerOf=fr");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Native Speaker", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersByCountryOfBirth()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Spanish Born", subjectId, countryOfBirthCode: "ES"),
            MakeDocument("French Born", subjectId, countryOfBirthCode: "FR"));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&countryOfBirthCode=ES");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Spanish Born", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersByProfessionalTutorBadge()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Pro", subjectId, isProfessionalTutor: true),
            MakeDocument("NotPro", subjectId, isProfessionalTutor: false));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&professionalTutor=true");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Pro", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_FiltersBySuperTutorBadge()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Super", subjectId, isSuperTutor: true),
            MakeDocument("NotSuper", subjectId, isSuperTutor: false));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&superTutor=true");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Super", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_SortsByPriceAscending()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("B", subjectId, hourlyRateMinorUnits: 3000),
            MakeDocument("A", subjectId, hourlyRateMinorUnits: 1000));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&sort=PriceAsc");

        Assert.NotNull(response);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal("A", response.Items[0].DisplayName);
        Assert.Equal("B", response.Items[1].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_SortsByNewest_UsingPublishedAtUtc_NotRandomGuidOrder()
    {
        var subjectId = Guid.NewGuid();
        var older = MakeDocument("Older", subjectId, publishedAtUtc: DateTimeOffset.UtcNow.AddDays(-10));
        var newer = MakeDocument("Newer", subjectId, publishedAtUtc: DateTimeOffset.UtcNow);
        await SeedAsync(older, newer);

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&sort=Newest");

        Assert.NotNull(response);
        Assert.Equal("Newer", response.Items[0].DisplayName);
        Assert.Equal("Older", response.Items[1].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_RelevanceSort_WithKeyword_RanksCloserNameMatchFirst()
    {
        var subjectId = Guid.NewGuid();
        // Both have a low rating so a rating-only fallback would rank them arbitrarily —
        // only text relevance to "Maria" should be able to put the closer match first.
        await SeedAsync(
            MakeDocument("Maria Garcia", subjectId, averageRating: 3.0m),
            MakeDocument("Someone Else", subjectId, averageRating: 3.0m));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&q=Maria&sort=Relevance");

        Assert.NotNull(response);
        Assert.Equal("Maria Garcia", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_KeywordSearch_MatchesHeadlineToo()
    {
        var subjectId = Guid.NewGuid();
        var doc = MakeDocument("Jane Smith", subjectId);
        doc.Headline = "Specializes in unusual-keyword-xyz conversation practice";
        await SeedAsync(doc, MakeDocument("Other Tutor", subjectId));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&q=unusual-keyword-xyz");

        Assert.NotNull(response);
        Assert.Single(response.Items);
        Assert.Equal("Jane Smith", response.Items[0].DisplayName);
    }

    [Fact]
    public async Task SearchTutors_PaginatesResults()
    {
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("One", subjectId),
            MakeDocument("Two", subjectId),
            MakeDocument("Three", subjectId));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&page=1&pageSize=2");

        Assert.NotNull(response);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(3, response.TotalCount);
    }

    [Fact]
    public async Task SearchTutors_PaginationIsStable_AcrossPages_WhenManyRowsTieOnSortKey()
    {
        // Regression test: sorting without a deterministic tie-breaker means Skip/Take can
        // return a row on two different pages, or skip one entirely, when many rows share the
        // same primary sort value (here: every row has the identical default rating).
        var subjectId = Guid.NewGuid();
        var documents = Enumerable.Range(0, 10).Select(i => MakeDocument($"Tutor{i}", subjectId, averageRating: 4.0m)).ToArray();
        await SeedAsync(documents);

        var seenIds = new HashSet<Guid>();
        for (var page = 1; page <= 5; page++)
        {
            var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}&page={page}&pageSize=2&sort=RatingDesc");
            Assert.NotNull(response);
            foreach (var item in response.Items)
            {
                Assert.True(seenIds.Add(item.TutorUserId), "Pagination returned the same tutor on two different pages.");
            }
        }

        Assert.Equal(10, seenIds.Count);
    }

    [Fact]
    public async Task SearchTutors_NoMatches_ReturnsEmptyItemsNot404()
    {
        var response = await _client.GetAsync(
            $"/api/search/tutors?subjectId={Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ResponseDto>(JsonOptions, TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Empty(body.Items);
    }

    [Fact]
    public async Task SearchTutors_PriceMaxBelowPriceMin_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync(
            "/api/search/tutors?priceMinMinorUnits=5000&priceMaxMinorUnits=1000",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchTutors_PageSizeAboveMax_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/search/tutors?pageSize=51", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchTutors_InvalidCountryOfBirthCodeLength_ReturnsValidationProblem()
    {
        var response = await _client.GetAsync("/api/search/tutors?countryOfBirthCode=ESP", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SearchTutors_NotAcceptingNewStudents_StillAppearsInResults_FieldReflectsIt()
    {
        // ADR-0024: a not-accepting tutor is never hidden from search — only the card's CTA
        // treatment changes, mirroring the Tutor Profile page's own existing precedent.
        var subjectId = Guid.NewGuid();
        await SeedAsync(
            MakeDocument("Accepting Tutor", subjectId, isAcceptingNewStudents: true),
            MakeDocument("Not Accepting Tutor", subjectId, isAcceptingNewStudents: false));

        var response = await GetAsync($"/api/search/tutors?subjectId={subjectId}");

        Assert.NotNull(response);
        Assert.Equal(2, response.Items.Count);
        Assert.True(response.Items.Single(i => i.DisplayName == "Accepting Tutor").IsAcceptingNewStudents);
        Assert.False(response.Items.Single(i => i.DisplayName == "Not Accepting Tutor").IsAcceptingNewStudents);
    }
}
