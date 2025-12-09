using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

using Moq;

using Nager.Country;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Infrastructure.Importing.Services;

using Shouldly;

namespace Pinventory.Pins.Infrastructure.UnitTests.Services;

public class StarredPlacesValidatorTests
{
    [Test]
    public async Task ValidateAsync_returns_Invalid_when_place_has_null_name()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace(null, "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0, DateTimeOffset.UtcNow,
            null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.Invalid);
    }

    [Test]
    public async Task ValidateAsync_returns_Invalid_when_place_has_null_address()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace("Place Name", "http://maps.google.com/?cid=123", null, Alpha2Code.PL, 1.0, 2.0, DateTimeOffset.UtcNow,
            null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.Invalid);
    }

    [Test]
    public async Task ValidateAsync_returns_Invalid_when_place_has_removed_place_comment()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace("Place Name", "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0,
            DateTimeOffset.UtcNow,
            "No location information is available for this saved place");

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.Invalid);
    }

    [Test]
    public async Task ValidateAsync_returns_Invalid_when_google_place_id_cannot_be_parsed()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace("Place Name", "https://invalid-url.com", "Address", Alpha2Code.PL, 1.0, 2.0, DateTimeOffset.UtcNow,
            null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.Invalid);
    }

    [Test]
    public async Task ValidateAsync_returns_New_when_place_is_valid_and_no_existing_pins_for_user()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace("Place Name", "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0,
            DateTimeOffset.UtcNow, null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.New);
    }

    [Test]
    public async Task ValidateAsync_returns_Exists_when_place_id_matches_existing_pin()
    {
        // Arrange
        var userId = "user-1";
        var (validator, dbContext, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync(userId);

        var existingPin = new Pin(userId, "Existing Place", new GooglePlaceId("123"),
            new Address("123 Main St", Alpha2Code.PL), new Location(1.0, 2.0), DateTimeOffset.UtcNow);
        await dbContext.Pins.AddAsync(existingPin);
        await dbContext.SaveChangesAsync();

        var place = new StarredPlace("Updated Name", "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0,
            DateTimeOffset.UtcNow, null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.Exists);
    }

    [Test]
    public async Task ValidateAsync_returns_Conflicted_when_name_matches_but_place_id_differs()
    {
        // Arrange
        var userId = "user-1";
        var (validator, dbContext, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync(userId);

        var existingPin = new Pin(userId, "Same Name", new GooglePlaceId("123"),
            new Address("123 Main St", Alpha2Code.PL), new Location(1.0, 2.0), DateTimeOffset.UtcNow);
        await dbContext.Pins.AddAsync(existingPin);
        await dbContext.SaveChangesAsync();

        var place = new StarredPlace("Same Name", "http://maps.google.com/?cid=456", "Address", Alpha2Code.PL, 3.0, 4.0,
            DateTimeOffset.UtcNow, null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.Conflicted);
    }

    [Test]
    public async Task ValidateAsync_returns_New_when_place_is_valid_and_does_not_match_existing_pins()
    {
        // Arrange
        var userId = "user-1";
        var (validator, dbContext, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync(userId);

        var existingPin = new Pin(userId, "Different Place", new GooglePlaceId("123"),
            new Address("123 Main St", Alpha2Code.PL), new Location(1.0, 2.0), DateTimeOffset.UtcNow);
        await dbContext.Pins.AddAsync(existingPin);
        await dbContext.SaveChangesAsync();

        var place = new StarredPlace("New Place", "http://maps.google.com/?cid=456", "Address", Alpha2Code.PL, 3.0, 4.0,
            DateTimeOffset.UtcNow, null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.New);
    }

    [Test]
    public async Task ValidateAsync_uses_cache_for_subsequent_calls_with_same_import()
    {
        // Arrange
        var userId = "user-1";
        var (validator, dbContext, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync(userId);

        var existingPin = new Pin(userId, "Existing Place", new GooglePlaceId("123"),
            new Address("123 Main St", Alpha2Code.PL), new Location(1.0, 2.0), DateTimeOffset.UtcNow);
        await dbContext.Pins.AddAsync(existingPin);
        await dbContext.SaveChangesAsync();

        var place1 = new StarredPlace("Place 1", "http://maps.google.com/?cid=456", "Address", Alpha2Code.PL, 1.0, 2.0,
            DateTimeOffset.UtcNow, null);
        var place2 = new StarredPlace("Place 2", "http://maps.google.com/?cid=789", "Address", Alpha2Code.PL, 3.0, 4.0,
            DateTimeOffset.UtcNow, null);

        // Act
        var result1 = await validator.ValidateAsync(import, place1);

        // Add another pin after first validation - should not be seen due to cache
        var newPin = new Pin(userId, "New Place", new GooglePlaceId("789"),
            new Address("456 Main St", Alpha2Code.PL), new Location(3.0, 4.0), DateTimeOffset.UtcNow);
        await dbContext.Pins.AddAsync(newPin);
        await dbContext.SaveChangesAsync();

        var result2 = await validator.ValidateAsync(import, place2);

        // Assert
        result1.ShouldBe(StarredPlaceState.New);
        result2.ShouldBe(StarredPlaceState.New); // Should be New because cache doesn't have the new pin
    }

    [Test]
    public async Task ValidateAsync_respects_cancellation_token()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace("Place Name", "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0,
            DateTimeOffset.UtcNow, null);
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await validator.ValidateAsync(import, place, cts.Token));
    }

    [Test]
    public async Task ValidateAsync_returns_New_when_cache_returns_null()
    {
        // Arrange
        var (validator, _, _) = await CreateValidatorAsync();
        var import = await CreateStartedImportAsync();
        var place = new StarredPlace("Place Name", "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0,
            DateTimeOffset.UtcNow, null);

        // Act
        var result = await validator.ValidateAsync(import, place);

        // Assert
        result.ShouldBe(StarredPlaceState.New);
    }

    private static async Task<(StarredPlacesValidator validator, PinsDbContext dbContext, IMemoryCache cache)> CreateValidatorAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var cache = new MemoryCache(new MemoryCacheOptions());
        var validator = new StarredPlacesValidator(dbContext, cache);

        return (validator, dbContext, cache);
    }

    private static async Task<Import> CreateStartedImportAsync(string userId = "user-1", string archiveJobId = "job-123")
    {
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await import.StartAsync(archiveJobId, policyMock.Object);
        return import;
    }
}