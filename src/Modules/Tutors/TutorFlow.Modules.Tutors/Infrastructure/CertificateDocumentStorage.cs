using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Tutors.Domain;

namespace TutorFlow.Modules.Tutors.Infrastructure;

/// <summary>Private certificate-document storage — deliberately NOT <see cref="TutorPhotoStorage"/>'s
/// pattern: a certificate document is a confidential credential, not a public-facing photo, so this
/// root lives outside <c>wwwroot</c> and is never mapped by <c>UseStaticFiles</c>. Local disk only,
/// same "no object-storage provider/deployment target exists yet" reasoning as
/// <see cref="TutorPhotoStorage"/> — not a production-readiness claim (see the module's own
/// production-gate note on malware scanning).
///
/// Validation is signature/extension/size only — no real image decode is performed. A
/// byte-valid-but-corrupt JPEG/PNG will pass this check undetected; this is a deliberate, approved
/// scope decision (SixLabors.ImageSharp was evaluated and explicitly declined), not an oversight.
/// Never claim decode-level corruption detection based on this class.</summary>
public interface ICertificateDocumentStorage
{
    Task<Result<string>> SaveAsync(IFormFile file, CancellationToken ct);

    /// <summary>Opens the file for reading, or null if it no longer exists on disk (e.g. already
    /// cleaned up). Never throws for a missing file — a missing file is a recoverable, reportable
    /// state, not an exceptional one.</summary>
    Stream? OpenRead(string fileReference);

    /// <summary>Best-effort delete. Returns false (never throws for a missing/already-deleted
    /// file) so callers can log a failure without conflating "already gone" with "delete failed".</summary>
    bool TryDelete(string fileReference);
}

public sealed class CertificateDocumentStorageOptions
{
    public string? RootPath { get; set; }
}

// Public (not the usual internal convention) — MaxFileSizeBytes/ResolveRootPath are referenced
// directly by integration tests, the same precedent as the public TutorPhotoStorage.
public sealed class LocalCertificateDocumentStorage(IWebHostEnvironment env, IOptions<CertificateDocumentStorageOptions> options)
    : ICertificateDocumentStorage
{
    public const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB — evidence-backed (Preply_Certifications screenshots).

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    public static string ResolveRootPath(IWebHostEnvironment env, string? configuredRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return configuredRootPath;
        }

        // ContentRootPath, never WebRootPath — this directory must not be reachable through the
        // static-file middleware under any configuration.
        return Path.Combine(env.ContentRootPath, "private-storage", "tutor-certificates");
    }

    /// <summary>JPEG/PNG only — evidence shows these two formats, not the three
    /// (JPEG/PNG/WebP) Stage 1's photo upload accepts. Extending to WebP here would be an
    /// unevidenced invention, not a parity fix.</summary>
    internal static async Task<string?> DetectImageExtensionAsync(IFormFile file, CancellationToken ct)
    {
        var header = new byte[8];
        await using var stream = file.OpenReadStream();
        var read = await stream.ReadAsync(header.AsMemory(0, header.Length), ct);

        if (read >= JpegSignature.Length && header.AsSpan(0, JpegSignature.Length).SequenceEqual(JpegSignature))
        {
            return ".jpg";
        }

        if (read >= PngSignature.Length && header.AsSpan(0, PngSignature.Length).SequenceEqual(PngSignature))
        {
            return ".png";
        }

        return null;
    }

    public async Task<Result<string>> SaveAsync(IFormFile file, CancellationToken ct)
    {
        if (file.Length == 0)
        {
            return Result.Failure<string>("No file was uploaded.");
        }

        // Checked against the declared length first (cheap, rejects an obviously-oversized
        // request before touching the stream), then enforced again below while actually copying
        // bytes — the declared Content-Length is never trusted alone.
        if (file.Length > MaxFileSizeBytes)
        {
            return Result.Failure<string>("Document must be 20 MB or smaller.");
        }

        var extension = await DetectImageExtensionAsync(file, ct);
        if (extension is null)
        {
            return Result.Failure<string>("Document must be a JPEG or PNG image.");
        }

        var root = ResolveRootPath(env, options.Value.RootPath);
        Directory.CreateDirectory(root);

        // Opaque, server-generated name — never the client's original filename, never derived
        // from any identifier that would let a filename alone reveal which tutor/certificate it
        // belongs to.
        var fileName = $"{Guid.NewGuid():N}{extension}";
        var finalPath = Path.Combine(root, fileName);
        var tempPath = Path.Combine(root, $"{fileName}.tmp");

        bool withinLimit;
        try
        {
            await using (var destination = File.Create(tempPath))
            await using (var source = file.OpenReadStream())
            {
                // Bounded copy: re-enforces the size cap against the actual byte stream, not just
                // the declared length, so a client lying about Content-Length cannot write past
                // the limit before this check catches it.
                withinLimit = await CopyWithLimitAsync(source, destination, MaxFileSizeBytes, ct);
            }
        }
        catch
        {
            TryDeleteTempFile(tempPath);
            throw;
        }

        if (!withinLimit)
        {
            TryDeleteTempFile(tempPath);
            return Result.Failure<string>("Document must be 20 MB or smaller.");
        }

        // Publish only after the full stream is validated and written — never a
        // partially-written file at the addressable final path.
        File.Move(tempPath, finalPath);
        return Result.Success(fileName);
    }

    public Stream? OpenRead(string fileReference)
    {
        var root = ResolveRootPath(env, options.Value.RootPath);
        var path = Path.Combine(root, fileReference);
        return File.Exists(path) ? File.OpenRead(path) : null;
    }

    public bool TryDelete(string fileReference)
    {
        var root = ResolveRootPath(env, options.Value.RootPath);
        var path = Path.Combine(root, fileReference);
        try
        {
            if (!File.Exists(path))
            {
                return false;
            }

            File.Delete(path);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <summary>Returns false (never throws) if the stream exceeds <paramref name="maxBytes"/> —
    /// the caller treats that as an ordinary validation failure, not an exceptional condition.</summary>
    private static async Task<bool> CopyWithLimitAsync(Stream source, Stream destination, long maxBytes, CancellationToken ct)
    {
        var buffer = new byte[81920];
        long totalRead = 0;
        int read;
        while ((read = await source.ReadAsync(buffer, ct)) > 0)
        {
            totalRead += read;
            if (totalRead > maxBytes)
            {
                return false;
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), ct);
        }

        return true;
    }

    private static void TryDeleteTempFile(string tempPath)
    {
        try
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
