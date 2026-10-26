using Microsoft.AspNetCore.Hosting;

namespace TutorFlow.Modules.Tutors.Infrastructure;

/// <summary>Local-disk photo storage: no object-storage provider (S3/Azure Blob/MinIO) exists
/// anywhere in this codebase, and this project has no defined production deployment target yet
/// (no Dockerfile for TutorFlow.Host, no cloud config) — local disk under wwwroot, served via
/// ASP.NET Core's built-in static file middleware, is the smallest viable choice until a real
/// deployment target makes a persistent volume or object storage necessary. Revisit then, not
/// speculatively now.</summary>
public static class TutorPhotoStorage
{
    public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

    /// <summary><c>configuredRootPath</c> lets tests point this at a temp directory instead of
    /// the real wwwroot — same resolution logic used both before the host builds (Program.cs,
    /// so the static-file provider isn't fixed to a NullFileProvider for a not-yet-existing
    /// directory) and inside the upload handler itself.</summary>
    public static string ResolveRootPath(IWebHostEnvironment env, string? configuredRootPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredRootPath))
        {
            return configuredRootPath;
        }

        var webRoot = env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");
        return Path.Combine(webRoot, "uploads", "tutor-photos");
    }
}

public sealed class TutorPhotoStorageOptions
{
    public string? RootPath { get; set; }
}
