using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AddFavoriteResponse = TutorFlow.Modules.Students.Features.AddFavorite.AddFavorite.Response;
using GetFavoriteStatusResponse = TutorFlow.Modules.Students.Features.GetFavoriteStatus.GetFavoriteStatus.Response;
using GetFavoriteStatusesResponse = TutorFlow.Modules.Students.Features.GetFavoriteStatuses.GetFavoriteStatuses.Response;

namespace TutorFlow.Modules.Students.Tests;

public class FavoritesEndpointsTests(StudentsApiFixture fixture) : IClassFixture<StudentsApiFixture>
{
    private HttpClient CreateAuthenticatedClient(Guid userId, params string[] roles)
    {
        var client = fixture.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", StudentsApiFixture.CreateAccessToken(userId, roles));
        return client;
    }

    [Fact]
    public async Task GetFavoriteStatus_WhenNotFavorited_ReturnsFalse()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var tutorUserId = Guid.NewGuid();

        var response = await client.GetAsync($"/api/students/me/favorites/{tutorUserId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetFavoriteStatusResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.False(body!.IsFavorited);
    }

    [Fact]
    public async Task AddFavorite_ThenGetStatus_ReturnsTrue()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var tutorUserId = Guid.NewGuid();

        var addResponse = await client.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, addResponse.StatusCode);
        var addBody = await addResponse.Content.ReadFromJsonAsync<AddFavoriteResponse>(TestContext.Current.CancellationToken);
        Assert.True(addBody!.IsFavorited);

        var statusResponse = await client.GetAsync($"/api/students/me/favorites/{tutorUserId}", TestContext.Current.CancellationToken);
        var statusBody = await statusResponse.Content.ReadFromJsonAsync<GetFavoriteStatusResponse>(TestContext.Current.CancellationToken);
        Assert.True(statusBody!.IsFavorited);
    }

    [Fact]
    public async Task AddFavorite_CalledTwice_IsIdempotent()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var tutorUserId = Guid.NewGuid();

        var first = await client.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);
        var second = await client.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task RemoveFavorite_AfterAdding_ClearsStatus()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var tutorUserId = Guid.NewGuid();
        await client.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);

        var removeResponse = await client.DeleteAsync($"/api/students/me/favorites/{tutorUserId}", TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, removeResponse.StatusCode);

        var statusResponse = await client.GetAsync($"/api/students/me/favorites/{tutorUserId}", TestContext.Current.CancellationToken);
        var statusBody = await statusResponse.Content.ReadFromJsonAsync<GetFavoriteStatusResponse>(TestContext.Current.CancellationToken);
        Assert.False(statusBody!.IsFavorited);
    }

    [Fact]
    public async Task RemoveFavorite_WhenNotFavorited_IsIdempotentNoContent()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var tutorUserId = Guid.NewGuid();

        var response = await client.DeleteAsync($"/api/students/me/favorites/{tutorUserId}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_WithoutStudentRole_ReturnsForbidden()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Tutor");
        var tutorUserId = Guid.NewGuid();

        var response = await client.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddFavorite_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.CreateClient();
        var tutorUserId = Guid.NewGuid();

        var response = await client.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Favorites_AreScopedPerStudent()
    {
        var tutorUserId = Guid.NewGuid();
        var studentA = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var studentB = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        await studentA.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);

        var statusForB = await studentB.GetAsync($"/api/students/me/favorites/{tutorUserId}", TestContext.Current.CancellationToken);
        var bodyForB = await statusForB.Content.ReadFromJsonAsync<GetFavoriteStatusResponse>(TestContext.Current.CancellationToken);

        Assert.False(bodyForB!.IsFavorited);
    }

    [Fact]
    public async Task GetFavoriteStatuses_ReturnsOnlyTheRequestedIdsThatAreFavorited()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var favorited = Guid.NewGuid();
        var alsoFavoritedButNotRequested = Guid.NewGuid();
        var notFavorited = Guid.NewGuid();
        var neverHeardOf = Guid.NewGuid();

        await client.PutAsync($"/api/students/me/favorites/{favorited}", null, TestContext.Current.CancellationToken);
        await client.PutAsync($"/api/students/me/favorites/{alsoFavoritedButNotRequested}", null, TestContext.Current.CancellationToken);

        var response = await client.GetAsync(
            $"/api/students/me/favorites?tutorUserIds={favorited},{notFavorited},{neverHeardOf}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetFavoriteStatusesResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Single(body!.FavoritedTutorUserIds);
        Assert.Contains(favorited, body.FavoritedTutorUserIds);
        Assert.DoesNotContain(alsoFavoritedButNotRequested, body.FavoritedTutorUserIds);
    }

    [Fact]
    public async Task GetFavoriteStatuses_WithNoIds_ReturnsEmptyWithoutQuerying()
    {
        var client = CreateAuthenticatedClient(Guid.NewGuid(), "Student");

        var response = await client.GetAsync("/api/students/me/favorites", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GetFavoriteStatusesResponse>(TestContext.Current.CancellationToken);
        Assert.NotNull(body);
        Assert.Empty(body!.FavoritedTutorUserIds);
    }

    [Fact]
    public async Task GetFavoriteStatuses_Unauthenticated_ReturnsUnauthorized()
    {
        var client = fixture.CreateClient();

        var response = await client.GetAsync(
            $"/api/students/me/favorites?tutorUserIds={Guid.NewGuid()}",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetFavoriteStatuses_AreScopedPerStudent()
    {
        var tutorUserId = Guid.NewGuid();
        var studentA = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        var studentB = CreateAuthenticatedClient(Guid.NewGuid(), "Student");
        await studentA.PutAsync($"/api/students/me/favorites/{tutorUserId}", null, TestContext.Current.CancellationToken);

        var response = await studentB.GetAsync(
            $"/api/students/me/favorites?tutorUserIds={tutorUserId}",
            TestContext.Current.CancellationToken);

        var body = await response.Content.ReadFromJsonAsync<GetFavoriteStatusesResponse>(TestContext.Current.CancellationToken);
        Assert.Empty(body!.FavoritedTutorUserIds);
    }
}
