using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Api.Tags.Dtos;
using Pinventory.Pins.Domain.Tags;
using Pinventory.Testing.Authorization;

using Shouldly;

namespace Pinventory.Pins.Api.IntegrationTests.Tags;

[NotInParallel]
public class TagsEndpointsAuthorizationTests
{
    private const string AdminUserId = AuthenticationTestHandler.AdminUserId;
    private const string OtherUserId = "other-user-id";

    [ClassDataSource<PinsApiTestApplication>(Shared = SharedType.PerTestSession)]
    public required PinsApiTestApplication App { get; init; }

    [After(Test)]
    public async Task CleanUpAsync()
    {
        App.CurrentUserId = AuthenticationTestHandler.TestUserId;
        await App.ResetDatabaseAsync();
    }

    [Test]
    public async Task GetTags_returns_forbidden_when_ownerId_does_not_match_authenticated_user()
    {
        // Arrange
        var ownerId = OtherUserId;
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();

        // Act - Using a default client with TestUserId
        var response = await App.Client.GetAsync($"/tags/{ownerId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DefineTags_allows_admin_to_create_global_catalog()
    {
        // Arrange
        App.CurrentUserId = AdminUserId;
        var tags = new TagsDto(["restaurant", "cafe", "bar"]);

        // Act - Admin creating global catalog (ownerId parameter omitted from route)
        var response = await App.Client.PostAsJsonAsync("/tags", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Test]
    public async Task DefineTags_returns_forbidden_when_non_admin_tries_to_create_global_catalog()
    {
        // Arrange
        var tags = new TagsDto(["restaurant", "cafe", "bar"]);

        // Act - Non-admin user trying to create global catalog (ownerId parameter omitted from route)
        var response = await App.Client.PostAsJsonAsync("/tags", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DefineTags_returns_forbidden_when_ownerId_does_not_match_authenticated_user()
    {
        // Arrange
        var ownerId = OtherUserId;
        var tags = new TagsDto(["restaurant", "cafe", "bar"]);

        // Act - Using a default client with TestUserId, trying to create catalog for OtherUserId
        var response = await App.Client.PostAsJsonAsync($"/tags/{ownerId}", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AddTag_allows_admin_to_modify_global_catalog()
    {
        // Arrange
        App.CurrentUserId = AdminUserId;
        var catalog = new TagCatalog(); // Global catalog
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var newTag = new TagDto("bar");

        // Act - Admin adding tag to global catalog
        var response = await App.Client.PutAsJsonAsync("/tags", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify in database
        var updatedCatalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == null);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "cafe", "bar"], ignoreOrder: true);
    }

    [Test]
    public async Task AddTag_returns_forbidden_when_non_admin_tries_to_modify_global_catalog()
    {
        // Arrange
        var catalog = new TagCatalog(); // Global catalog
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var newTag = new TagDto("bar");

        // Act - Non-admin user trying to modify global catalog
        var response = await App.Client.PutAsJsonAsync("/tags", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AddTag_returns_forbidden_when_ownerId_does_not_match_authenticated_user()
    {
        // Arrange
        var ownerId = OtherUserId;
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var newTag = new TagDto("bar");

        // Act - Using a default client with TestUserId, trying to modify OtherUserId's catalog
        var response = await App.Client.PutAsJsonAsync($"/tags/{ownerId}", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task RemoveTag_allows_admin_to_modify_global_catalog()
    {
        // Arrange
        App.CurrentUserId = AdminUserId;
        var catalog = new TagCatalog(); // Global catalog
        catalog.DefineTags(["restaurant", "cafe", "bar"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var tagToRemove = new TagDto("cafe");

        // Act - Admin removing tag from global catalog
        var request = new HttpRequestMessage(HttpMethod.Delete, "/tags") { Content = JsonContent.Create(tagToRemove) };
        var response = await App.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify in database
        var updatedCatalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == null);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "bar"], ignoreOrder: true);
        request.Dispose();
    }

    [Test]
    public async Task RemoveTag_returns_forbidden_when_non_admin_tries_to_modify_global_catalog()
    {
        // Arrange
        var catalog = new TagCatalog(); // Global catalog
        catalog.DefineTags(["restaurant", "cafe", "bar"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var tagToRemove = new TagDto("cafe");

        // Act - Non-admin user trying to remove tag from global catalog
        var request = new HttpRequestMessage(HttpMethod.Delete, "/tags") { Content = JsonContent.Create(tagToRemove) };
        var response = await App.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        request.Dispose();
    }

    [Test]
    public async Task RemoveTag_returns_forbidden_when_ownerId_does_not_match_authenticated_user()
    {
        // Arrange
        var ownerId = OtherUserId;
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe", "bar"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var tagToRemove = new TagDto("cafe");

        // Act - Using a default client with TestUserId, trying to modify OtherUserId's catalog
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/tags/{ownerId}") { Content = JsonContent.Create(tagToRemove) };
        var response = await App.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        request.Dispose();
    }

    [Test]
    public async Task User_can_access_own_catalog_with_matching_ownerId()
    {
        // Arrange
        App.CurrentUserId = OtherUserId;
        var catalog = new TagCatalog(OtherUserId);
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var newTag = new TagDto("bar");

        // Act - User accessing their own catalog
        var response = await App.Client.PutAsJsonAsync($"/tags/{OtherUserId}", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify in database
        var updatedCatalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == OtherUserId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "cafe", "bar"], ignoreOrder: true);
    }
}