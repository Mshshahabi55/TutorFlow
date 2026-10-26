using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using TutorFlow.Modules.Tutors.Domain;
using TutorFlow.Modules.Tutors.Infrastructure;

namespace TutorFlow.Modules.Tutors.Features.UploadProfilePhoto;

public static class UploadProfilePhoto
{
    public sealed record Response(string FileName);

    private static readonly byte[] JpegSignature = [0xFF, 0xD8, 0xFF];
    private static readonly byte[] PngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>Resolves the stored file's real format from its own bytes, not the client-supplied
    /// content-type (which is trivially spoofable) — JPEG/PNG/WebP only, matching the approved
    /// upload-limits decision. Returns the extension to save under, or null if the file isn't a
    /// recognized image format.</summary>
    internal static async Task<string?> DetectImageExtensionAsync(IFormFile file, CancellationToken ct)
    {
        var header = new byte[12];
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

        // WebP container: "RIFF" + 4-byte size + "WEBP".
        if (read >= 12 && header.AsSpan(0, 4).SequenceEqual("RIFF"u8) && header.AsSpan(8, 4).SequenceEqual("WEBP"u8))
        {
            return ".webp";
        }

        return null;
    }

    internal sealed class Handler(IWebHostEnvironment env, IOptions<TutorPhotoStorageOptions> options)
    {
        public async Task<Result<Response>> HandleAsync(Guid userId, IFormFile? file, CancellationToken ct)
        {
            if (file is null || file.Length == 0)
            {
                return Result.Failure<Response>("No file was uploaded.");
            }

            if (file.Length > TutorPhotoStorage.MaxFileSizeBytes)
            {
                return Result.Failure<Response>("Photo must be 5 MB or smaller.");
            }

            var extension = await DetectImageExtensionAsync(file, ct);
            if (extension is null)
            {
                return Result.Failure<Response>("Photo must be a JPEG, PNG, or WebP image.");
            }

            var root = TutorPhotoStorage.ResolveRootPath(env, options.Value.RootPath);
            Directory.CreateDirectory(root);

            // One photo per tutor: remove any previously-saved file for this user (any
            // extension) before writing the new one, so storage never grows unbounded and no
            // separate orphan-cleanup job is needed.
            foreach (var existing in Directory.EnumerateFiles(root, $"{userId:N}.*"))
            {
                File.Delete(existing);
            }

            var fileName = $"{userId:N}{extension}";
            var path = Path.Combine(root, fileName);
            await using (var destination = File.Create(path))
            await using (var source = file.OpenReadStream())
            {
                await source.CopyToAsync(destination, ct);
            }

            return Result.Success(new Response(fileName));
        }
    }
}
