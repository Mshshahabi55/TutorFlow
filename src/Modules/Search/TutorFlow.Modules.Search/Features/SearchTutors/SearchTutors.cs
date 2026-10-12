using FluentValidation;
using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Search.Domain;
using TutorFlow.Modules.Search.Persistence;

namespace TutorFlow.Modules.Search.Features.SearchTutors;

public static class SearchTutors
{
    public enum SortOption
    {
        Relevance,
        RatingDesc,
        PriceAsc,
        PriceDesc,
        Newest,
    }

    /// <summary>
    /// Models the Preply-parity product surface (`PREPLY_SEARCH_PARITY_MATRIX.md`), not the
    /// accidental shape of whatever `Tutors`/`Subjects` happened to store first (ADR-0021) —
    /// catalog concepts (subject, specialty) are IDs; free text is reserved for `q` only.
    /// </summary>
    public sealed record Query(
        string? Q,
        Guid? SubjectId,
        string? SpecialtyIds,
        string? SpokenLanguageCodes,
        string? NativeSpeakerOf,
        string? CountryOfBirthCode,
        bool? ProfessionalTutor,
        bool? SuperTutor,
        int? PriceMinMinorUnits,
        int? PriceMaxMinorUnits,
        decimal? MinRating,
        bool? AvailableSoon,
        SortOption? Sort,
        // Tutor Profile's "Similar tutors" section reuses this endpoint (same subject, minus
        // the profile being viewed) rather than a bespoke recommendation query — ADR-0022,
        // PREPLY_TUTOR_PROFILE_PARITY_SPEC.md §8.
        Guid? ExcludeTutorUserId = null,
        int Page = 1,
        int PageSize = 20)
    {
        public IReadOnlyList<Guid> SpecialtyIdList => SplitGuids(SpecialtyIds);

        public IReadOnlyList<string> SpokenLanguageCodeList => SplitStrings(SpokenLanguageCodes);

        private static IReadOnlyList<string> SplitStrings(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? []
                : value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        private static IReadOnlyList<Guid> SplitGuids(string? value) =>
            SplitStrings(value)
                .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
                .Where(g => g is not null)
                .Select(g => g!.Value)
                .ToList();
    }

    public sealed record SpokenLanguageResponse(string LanguageCode, string Proficiency);

    public sealed record TutorSearchResultResponse(
        Guid TutorUserId,
        string Slug,
        string DisplayName,
        string Headline,
        string? PhotoUrl,
        IReadOnlyList<Guid> SubjectIds,
        IReadOnlyList<Guid> SpecialtyIds,
        IReadOnlyList<SpokenLanguageResponse> SpokenLanguages,
        string? CountryOfBirthCode,
        bool IsProfessionalTutor,
        bool IsSuperTutor,
        int HourlyRateMinorUnits,
        string Currency,
        decimal AverageRating,
        int ReviewCount,
        int LessonCount,
        bool HasUpcomingAvailability,
        bool IsAcceptingNewStudents);

    public sealed record Response(IReadOnlyList<TutorSearchResultResponse> Items, int Page, int PageSize, int TotalCount);

    public sealed class Validator : AbstractValidator<Query>
    {
        public const int MaxPageSize = 50;

        public Validator()
        {
            RuleFor(q => q.Page).GreaterThanOrEqualTo(1);
            RuleFor(q => q.PageSize).InclusiveBetween(1, MaxPageSize);
            RuleFor(q => q.MinRating).InclusiveBetween(0m, 5m).When(q => q.MinRating is not null);
            RuleFor(q => q.PriceMaxMinorUnits)
                .GreaterThanOrEqualTo(q => q.PriceMinMinorUnits)
                .When(q => q.PriceMinMinorUnits is not null && q.PriceMaxMinorUnits is not null)
                .WithMessage("priceMaxMinorUnits must be greater than or equal to priceMinMinorUnits.");
            RuleFor(q => q.PriceMinMinorUnits).GreaterThanOrEqualTo(0).When(q => q.PriceMinMinorUnits is not null);
            RuleFor(q => q.PriceMaxMinorUnits).GreaterThanOrEqualTo(0).When(q => q.PriceMaxMinorUnits is not null);
            RuleFor(q => q.CountryOfBirthCode!).Length(2).When(q => q.CountryOfBirthCode is not null);
            RuleFor(q => q.NativeSpeakerOf!).Length(2).When(q => q.NativeSpeakerOf is not null);
        }
    }

    internal sealed class Handler(SearchDbContext db)
    {
        public async Task<Response> HandleAsync(Query query, CancellationToken ct)
        {
            IQueryable<TutorSearchDocument> documents = db.TutorSearchDocuments.AsNoTracking().Where(d => d.IsPublished);

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var pattern = $"%{query.Q}%";
                documents = documents.Where(d => EF.Functions.ILike(d.DisplayName, pattern) || EF.Functions.ILike(d.Headline, pattern));
            }

            if (query.SubjectId is { } subjectId)
            {
                documents = documents.Where(d => d.SubjectIds.Contains(subjectId));
            }

            foreach (var specialtyId in query.SpecialtyIdList)
            {
                // AND semantics (tutor must have every requested specialty) — OPEN_QUESTIONS.md #36.
                documents = documents.Where(d => d.SpecialtyIds.Contains(specialtyId));
            }

            if (query.SpokenLanguageCodeList.Count > 0)
            {
                // Overlap ("speaks any of these") — OR of per-code `Contains` checks, the more
                // reliably-translated shape for the Npgsql array provider.
                var codes = query.SpokenLanguageCodeList;
                documents = documents.Where(d => codes.Any(code => d.SpokenLanguageCodes.Contains(code)));
            }

            if (!string.IsNullOrWhiteSpace(query.NativeSpeakerOf))
            {
                documents = documents.Where(d => d.NativeLanguageCodes.Contains(query.NativeSpeakerOf));
            }

            if (!string.IsNullOrWhiteSpace(query.CountryOfBirthCode))
            {
                documents = documents.Where(d => d.CountryOfBirthCode == query.CountryOfBirthCode);
            }

            if (query.ProfessionalTutor == true)
            {
                documents = documents.Where(d => d.IsProfessionalTutor);
            }

            if (query.SuperTutor == true)
            {
                documents = documents.Where(d => d.IsSuperTutor);
            }

            if (query.PriceMinMinorUnits is { } priceMin)
            {
                documents = documents.Where(d => d.HourlyRateMinorUnits >= priceMin);
            }

            if (query.PriceMaxMinorUnits is { } priceMax)
            {
                documents = documents.Where(d => d.HourlyRateMinorUnits <= priceMax);
            }

            if (query.MinRating is { } minRating)
            {
                documents = documents.Where(d => d.AverageRating >= minRating);
            }

            if (query.AvailableSoon == true)
            {
                documents = documents.Where(d => d.HasUpcomingAvailability);
            }

            if (query.ExcludeTutorUserId is { } excludeTutorUserId)
            {
                documents = documents.Where(d => d.TutorUserId != excludeTutorUserId);
            }

            // Every branch ends with `.ThenBy(TutorUserId)` — a deterministic tie-breaker.
            // Without one, rows that tie on the primary key (common: many untouched documents
            // share the same default rating/price) have no guaranteed stable order, so
            // Skip/Take pagination can silently duplicate or skip rows across pages. Found
            // during hardening review; regression-tested in SearchEndpointsTests.
            var hasQuery = !string.IsNullOrWhiteSpace(query.Q);
            documents = (query.Sort ?? SortOption.Relevance) switch
            {
                SortOption.RatingDesc => documents.OrderByDescending(d => d.AverageRating).ThenBy(d => d.TutorUserId),
                SortOption.PriceAsc => documents.OrderBy(d => d.HourlyRateMinorUnits).ThenBy(d => d.TutorUserId),
                SortOption.PriceDesc => documents.OrderByDescending(d => d.HourlyRateMinorUnits).ThenBy(d => d.TutorUserId),
                SortOption.Newest => documents.OrderByDescending(d => d.PublishedAtUtc).ThenBy(d => d.TutorUserId),
                // Relevance: rank by trigram similarity to the free-text query when one is
                // given (pg_trgm — the same extension/index the `q` filter above already
                // uses), falling back to rating when there's no text to rank against. The
                // original implementation had two identical branches here and never actually
                // ranked by text relevance at all — found during hardening review.
                _ => hasQuery
                    ? documents
                        .OrderByDescending(d => EF.Functions.TrigramsSimilarity(d.DisplayName, query.Q!))
                        .ThenByDescending(d => d.AverageRating)
                        .ThenBy(d => d.TutorUserId)
                    : documents.OrderByDescending(d => d.AverageRating).ThenBy(d => d.TutorUserId),
            };

            var totalCount = await documents.CountAsync(ct);

            // Materialize the flat columns first, then build the nested SpokenLanguages shape
            // in memory — constructing a record per array element inside an EF `.Select()`
            // risks a "could not be translated" failure on the Npgsql provider; this avoids it.
            var rows = await documents
                .Skip((query.Page - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync(ct);

            var items = rows
                .Select(d => new TutorSearchResultResponse(
                    d.TutorUserId,
                    d.Slug,
                    d.DisplayName,
                    d.Headline,
                    d.PhotoUrl,
                    d.SubjectIds,
                    d.SpecialtyIds,
                    d.SpokenLanguageCodes
                        .Select(c => new SpokenLanguageResponse(c, d.NativeLanguageCodes.Contains(c) ? "Native" : "Unspecified"))
                        .ToList(),
                    d.CountryOfBirthCode,
                    d.IsProfessionalTutor,
                    d.IsSuperTutor,
                    d.HourlyRateMinorUnits,
                    d.Currency,
                    d.AverageRating,
                    d.ReviewCount,
                    d.LessonCount,
                    d.HasUpcomingAvailability,
                    d.IsAcceptingNewStudents))
                .ToList();

            return new Response(items, query.Page, query.PageSize, totalCount);
        }
    }
}
