using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Infrastructure.Tags;

using Shouldly;

namespace Pinventory.Pins.Infrastructure.UnitTests.Services;

public class TagVerifierTests
{
    [Test]
    public async Task IsAllowedAsync_returns_false_when_tag_is_null()
    {
        // Arrange
        var (verifier, _, _) = await CreateVerifierAsync();
        var ownerId = "user-1";

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, null!);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsAllowedAsync_returns_false_when_tag_is_empty_string()
    {
        // Arrange
        var (verifier, _, _) = await CreateVerifierAsync();
        var ownerId = "user-1";

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "");

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsAllowedAsync_returns_false_when_tag_is_whitespace()
    {
        // Arrange
        var (verifier, _, _) = await CreateVerifierAsync();
        var ownerId = "user-1";

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "   ");

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsAllowedAsync_returns_true_when_tag_exists_in_user_catalog()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe", "bar"]);
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "restaurant");

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task IsAllowedAsync_returns_true_when_tag_exists_case_insensitive()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe"]);
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "RESTAURANT");

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task IsAllowedAsync_returns_false_when_tag_does_not_exist_in_user_catalog()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe"]);
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "museum");

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsAllowedAsync_returns_false_when_no_catalog_exists_for_user()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, _, _) = await CreateVerifierAsync();

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "restaurant");

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsAllowedAsync_works_with_global_catalog()
    {
        // Arrange
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var globalCatalog = new TagCatalog();
        globalCatalog.DefineTags(["restaurant", "cafe", "museum"]);
        await dbContext.TagCatalogs.AddAsync(globalCatalog);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await verifier.IsAllowedAsync(null, "museum");

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task IsAllowedAsync_normalizes_tag_with_trim_and_lowercase()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant"]);
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "  RESTAURANT  ");

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task IsAllowedAsync_uses_cache_for_subsequent_calls()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["restaurant", "cafe"]);
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();

        // Act
        var result1 = await verifier.IsAllowedAsync(ownerId, "restaurant");

        // Add a new tag to the catalog after first call - should not be seen due to cache
        var updatedCatalog = await dbContext.TagCatalogs.FirstAsync(c => c.OwnerId == ownerId);
        updatedCatalog.AddTag("museum");
        await dbContext.SaveChangesAsync();

        var result2 = await verifier.IsAllowedAsync(ownerId, "museum");

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeFalse(); // Should be false because cache doesn't have the new tag
    }

    [Test]
    public async Task IsAllowedAsync_respects_cancellation_token()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, _, _) = await CreateVerifierAsync();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await verifier.IsAllowedAsync(ownerId, "restaurant", cts.Token));
    }

    [Test]
    public async Task IsAllowedAsync_returns_false_when_cache_returns_null()
    {
        // Arrange
        var ownerId = "user-1";
        var (verifier, _, _) = await CreateVerifierAsync();

        // Act
        var result = await verifier.IsAllowedAsync(ownerId, "restaurant");

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task IsAllowedAsync_handles_multiple_catalogs_for_different_owners()
    {
        // Arrange
        var ownerId1 = "user-1";
        var ownerId2 = "user-2";
        var (verifier, dbContext, _) = await CreateVerifierAsync();

        var catalog1 = new TagCatalog(ownerId1);
        catalog1.DefineTags(["restaurant"]);
        var catalog2 = new TagCatalog(ownerId2);
        catalog2.DefineTags(["museum"]);

        await dbContext.TagCatalogs.AddRangeAsync(catalog1, catalog2);
        await dbContext.SaveChangesAsync();

        // Act
        var result1 = await verifier.IsAllowedAsync(ownerId1, "restaurant");
        var result2 = await verifier.IsAllowedAsync(ownerId1, "museum");
        var result3 = await verifier.IsAllowedAsync(ownerId2, "restaurant");
        var result4 = await verifier.IsAllowedAsync(ownerId2, "museum");

        // Assert
        result1.ShouldBeTrue();
        result2.ShouldBeFalse();
        result3.ShouldBeFalse();
        result4.ShouldBeTrue();
    }

    private static async Task<(TagVerifier verifier, PinsDbContext dbContext, IMemoryCache cache)> CreateVerifierAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var verifier = new TagVerifier(dbContext, cache);

        return (verifier, dbContext, cache);
    }
}