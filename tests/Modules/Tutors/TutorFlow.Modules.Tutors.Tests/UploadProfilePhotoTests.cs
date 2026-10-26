using System.Net;
using System.Net.Http.Headers;

namespace TutorFlow.Modules.Tutors.Tests;

[Collection(TutorsHostCollection.Name)]
public class UploadProfilePhotoTests(TutorsApiFixture fixture) : IClassFixture<TutorsApiFixture>
{
    // 1x1 pixel images, real magic bytes — not placeholder text, so the server's own
    // content-sniffing (not just the client-supplied Content-Type) is genuinely exercised.
    private static readonly byte[] ValidJpegBytes =
        [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, 0x01, 0xFF, 0xD9];

    private static readonly byte[] ValidPngBytes =
        [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TutorsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    private static MultipartFormDataContent BuildUpload(byte[] bytes, string fileName, string contentType)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        return content;
    }

    [Fact]
    public async Task UploadPhoto_WithValidJpeg_ReturnsOkWithAbsoluteUrl()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");

        var response = await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(ValidJpegBytes, "photo.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        Assert.Contains("/uploads/tutor-photos/", body);
        Assert.Contains(".jpg", body);
    }

    [Fact]
    public async Task UploadPhoto_WithValidPng_SavesFileToDisk()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");

        var response = await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(ValidPngBytes, "photo.png", "image/png"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var expectedPath = Path.Combine(fixture.PhotoStorageRootPath, $"{userId:N}.png");
        Assert.True(File.Exists(expectedPath));
    }

    [Fact]
    public async Task UploadPhoto_ReplacingAPreviousUpload_DeletesTheOldFile()
    {
        var userId = Guid.NewGuid();
        var client = CreateAuthenticatedClient(userId, "Tutor");

        await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(ValidJpegBytes, "first.jpg", "image/jpeg"), TestContext.Current.CancellationToken);
        await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(ValidPngBytes, "second.png", "image/png"), TestContext.Current.CancellationToken);

        Assert.False(File.Exists(Path.Combine(fixture.PhotoStorageRootPath, $"{userId:N}.jpg")));
        Assert.True(File.Exists(Path.Combine(fixture.PhotoStorageRootPath, $"{userId:N}.png")));
    }

    [Fact]
    public async Task UploadPhoto_WithDisallowedFileContent_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var notAnImage = "this is plain text, not an image"u8.ToArray();

        var response = await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(notAnImage, "fake.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_ExceedingSizeLimit_ReturnsBadRequest()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var oversized = new byte[Infrastructure.TutorPhotoStorage.MaxFileSizeBytes + 1];
        Array.Copy(ValidJpegBytes, oversized, ValidJpegBytes.Length);

        var response = await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(oversized, "big.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_WithoutTutorRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(ValidJpegBytes, "photo.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UploadPhoto_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.CreateClient();

        var response = await client.PostAsync(
            "/api/tutors/me/photo", BuildUpload(ValidJpegBytes, "photo.jpg", "image/jpeg"), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
