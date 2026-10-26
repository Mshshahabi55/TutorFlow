using Microsoft.EntityFrameworkCore;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;
using TutorFlow.Modules.Tutors.Persistence;

namespace TutorFlow.Modules.Tutors.Features.DownloadCertificateDocument;

/// <summary>Streams a certificate document for viewing — never a public URL, never static files.
/// Ownership/ authorization is re-checked on every call (the IDOR defense this feature exists to
/// provide), not cached from upload time. Shared by both the tutor's own "view my document" action
/// and the admin's "view for review" action — the two differ only in which application a caller is
/// allowed to address, enforced by the two distinct public methods' own queries, not by a shared
/// unchecked code path.</summary>
public static class DownloadCertificateDocument
{
    public sealed record Document(Stream Content, string ContentType);

    internal sealed class Handler(TutorsDbContext db, ICertificateDocumentStorage storage)
    {
        public async Task<Result<Document>> HandleForTutorAsync(Guid userId, Guid certificateId, CancellationToken ct)
        {
            var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.UserId == userId, ct);
            return await ResolveAsync(application, certificateId);
        }

        public async Task<Result<Document>> HandleForAdminAsync(Guid applicationId, Guid certificateId, CancellationToken ct)
        {
            var application = await db.TutorApplications.AsNoTracking().FirstOrDefaultAsync(a => a.Id == applicationId, ct);
            return await ResolveAsync(application, certificateId);
        }

        private Task<Result<Document>> ResolveAsync(TutorApplication? application, Guid certificateId)
        {
            if (application is null)
            {
                return Task.FromResult(Result.Failure<Document>("Tutor application not found."));
            }

            var entry = application.CertificateEntries.FirstOrDefault(e => e.Id == certificateId);
            if (entry is null || entry.FileReference is null)
            {
                return Task.FromResult(Result.Failure<Document>("This certificate has no document available."));
            }

            var stream = storage.OpenRead(entry.FileReference);
            if (stream is null)
            {
                return Task.FromResult(Result.Failure<Document>("This document is no longer available."));
            }

            var contentType = entry.FileReference.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg";
            return Task.FromResult(Result.Success(new Document(stream, contentType)));
        }
    }
}
