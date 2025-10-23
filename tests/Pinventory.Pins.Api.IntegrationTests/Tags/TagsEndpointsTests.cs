using System.Net;
using System.Net.Http.Json;

using Microsoft.EntityFrameworkCore;

using Pinventory.Pins.Api.Tags;
using Pinventory.Pins.Domain.Tags;

using Shouldly;

namespace Pinventory.Pins.Api.IntegrationTests.Tags;

[NotInParallel]
public class TagsEndpointsTests
{
    [ClassDataSource<PinsApiTestApplication>(Shared = SharedType.PerTestSession)]
    public required PinsApiTestApplication App { get; init; }

    [After(Test)]
    public async Task CleanUpAsync()
    {
        await App.ResetDatabaseAsync();
    }

    [Test]
    public async Task GetTags_returns_empty_list_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();

        // Act
        var response = await App.Client.GetAsync($"/tags/{ownerUserId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TagCatalogDto>();
        result.ShouldNotBeNull();
        result.Tags.ShouldBeEmpty();
    }

    [Test]
    public async Task GetTags_returns_tags_when_catalog_exists()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var catalog = new TagCatalog(ownerUserId);
        catalog.DefineTags(["restaurant", "cafe", "bar"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();

        // Act
        var response = await App.Client.GetAsync($"/tags/{ownerUserId}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TagCatalogDto>();
        result.ShouldNotBeNull();
        result.Tags.ShouldBe(["restaurant", "cafe", "bar"], ignoreOrder: true);
    }

    [Test]
    public async Task DefineTags_creates_new_catalog_and_returns_created()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        string[] tags = ["restaurant", "cafe", "bar"];

        // Act
        var response = await App.Client.PostAsJsonAsync($"/tags/{ownerUserId}/define", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<TagCatalogIdDto>();
        result.ShouldNotBeNull();
        result.OwnerUserId.ShouldBe(ownerUserId);
        result.InternalTagCatalogId.ShouldNotBe(Guid.Empty);

        // Verify in database
        var catalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
        catalog.ShouldNotBeNull();
        catalog.Tags.Select(t => t.Value).ShouldBe(tags, ignoreOrder: true);
    }

    [Test]
    public async Task DefineTags_returns_conflict_when_catalog_already_exists()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var existingCatalog = new TagCatalog(ownerUserId);
        existingCatalog.DefineTags(["existing"]);

        await App.DbContext.TagCatalogs.AddAsync(existingCatalog);
        await App.DbContext.SaveChangesAsync();

        string[] tags = ["restaurant", "cafe"];

        // Act
        var response = await App.Client.PostAsJsonAsync($"/tags/{ownerUserId}/define", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task DefineTags_normalizes_tags_by_trimming_and_deduplicating()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        string[] tags = [" restaurant ", "restaurant", "CAFE", "cafe", "  bar  "];

        // Act
        var response = await App.Client.PostAsJsonAsync($"/tags/{ownerUserId}/define", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify in database
        var catalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
        catalog.ShouldNotBeNull();
        catalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "cafe", "bar"], ignoreOrder: true);
    }

    [Test]
    public async Task AddTag_adds_tag_to_existing_catalog()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var catalog = new TagCatalog(ownerUserId);
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var newTag = "bar";

        // Act
        var response = await App.Client.PutAsJsonAsync($"/tags/{ownerUserId}", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify in database
        var updatedCatalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "cafe", "bar"], ignoreOrder: true);
    }

    [Test]
    public async Task AddTag_returns_not_found_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var newTag = "restaurant";

        // Act
        var response = await App.Client.PutAsJsonAsync($"/tags/{ownerUserId}", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddTag_returns_bad_request_when_tag_already_exists()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var catalog = new TagCatalog(ownerUserId);
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var duplicateTag = "restaurant";

        // Act
        var response = await App.Client.PutAsJsonAsync($"/tags/{ownerUserId}", duplicateTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task AddTag_normalizes_tag_value()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var catalog = new TagCatalog(ownerUserId);
        catalog.DefineTags(["restaurant"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var newTag = "  CAFE  ";

        // Act
        var response = await App.Client.PutAsJsonAsync($"/tags/{ownerUserId}", newTag);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify in database
        var updatedCatalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldContain("cafe");
    }

    [Test]
    public async Task RemoveTag_removes_tag_from_catalog()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var catalog = new TagCatalog(ownerUserId);
        catalog.DefineTags(["restaurant", "cafe", "bar"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var tagToRemove = "cafe";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/tags/{ownerUserId}")
        {
            Content = JsonContent.Create(tagToRemove)
        };
        var response = await App.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify in database
        var updatedCatalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "bar"], ignoreOrder: true);
    }

    [Test]
    public async Task RemoveTag_returns_not_found_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var tagToRemove = "restaurant";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/tags/{ownerUserId}")
        {
            Content = JsonContent.Create(tagToRemove)
        };
        var response = await App.Client.SendAsync(request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemoveTag_is_idempotent_when_tag_does_not_exist_in_catalog()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        var catalog = new TagCatalog(ownerUserId);
        catalog.DefineTags(["restaurant", "cafe"]);

        await App.DbContext.TagCatalogs.AddAsync(catalog);
        await App.DbContext.SaveChangesAsync();
        App.DbContext.ChangeTracker.Clear();

        var nonExistentTag = "bar";

        // Act
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/tags/{ownerUserId}")
        {
            Content = JsonContent.Create(nonExistentTag)
        };
        var response = await App.Client.SendAsync(request);

        // Assert - RemoveTag is idempotent, returns NoContent even if tag doesn't exist
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }



    [Test]
    public async Task DefineTags_filters_out_empty_and_whitespace_tags()
    {
        // Arrange
        var ownerUserId = Guid.NewGuid();
        string[] tags = ["restaurant", "", "   ", "cafe", "  ", "bar"];

        // Act
        var response = await App.Client.PostAsJsonAsync($"/tags/{ownerUserId}/define", tags);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Verify in database
        var catalog = await App.DbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerUserId == ownerUserId);
        catalog.ShouldNotBeNull();
        catalog.Tags.Select(t => t.Value).ShouldBe(["restaurant", "cafe", "bar"], ignoreOrder: true);
    }
}