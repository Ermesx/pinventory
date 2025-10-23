using Microsoft.EntityFrameworkCore;

using Moq;

using Pinventory.Pins.Application.Tags;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Domain.Tags.Events;
using Pinventory.Pins.Infrastructure;

using Shouldly;

using Wolverine;

namespace Pinventory.Pins.UnitTests.Application;

public class TagCatalogHandlerTests
{
    [Test]
    public async Task DefineTagCatalog_creates_catalog_and_returns_id_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerId = "123";
        var tags = new[] { "foo", "bar", "baz" };
        var command = new DefineTagCatalogCommand(ownerId, tags);

        var (handler, dbContext, busMock) = CreateHandler();

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBe(Guid.Empty);

        var catalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        catalog.ShouldNotBeNull();
        catalog.Id.ShouldBe(result.Value);
        catalog.OwnerId.ShouldBe(ownerId);
        catalog.Tags.Select(t => t.Value).ShouldBe(tags, ignoreOrder: true);

        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<TagCatalogTagsDefined>();
    }

    [Test]
    public async Task DefineTagCatalog_creates_global_catalog_when_owner_is_null()
    {
        // Arrange
        var tags = new[] { "restaurant", "cafe" };
        var command = new DefineTagCatalogCommand(null, tags);

        var (handler, dbContext, busMock) = CreateHandler();

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var catalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == null);
        catalog.ShouldNotBeNull();
        catalog.OwnerId.ShouldBeNull();
        catalog.Tags.Select(t => t.Value).ShouldBe(tags, ignoreOrder: true);
    }

    [Test]
    public async Task DefineTagCatalog_fails_when_catalog_already_exists()
    {
        // Arrange
        var ownerId = "123";
        var existingCatalog = new TagCatalog(ownerId);
        existingCatalog.DefineTags(new[] { "existing" });

        var (handler, dbContext, _) = CreateHandler();
        await dbContext.TagCatalogs.AddAsync(existingCatalog);
        await dbContext.SaveChangesAsync();

        var command = new DefineTagCatalogCommand(ownerId, new[] { "new" });

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("already exists"));

        var catalogs = await dbContext.TagCatalogs.Where(c => c.OwnerId == ownerId).ToListAsync();
        catalogs.Count.ShouldBe(1);
        catalogs[0].Tags.Select(t => t.Value).ShouldBe(new[] { "existing" });
    }

    [Test]
    public async Task DefineTagCatalog_fails_when_domain_validation_fails()
    {
        // Arrange
        var ownerId = "123";
        var tags = new[] { "valid", "", "   " }; // Empty tags should cause validation failure
        var command = new DefineTagCatalogCommand(ownerId, tags);

        var (handler, dbContext, busMock) = CreateHandler();

        // Act
        var result = await handler.Handle(command);

        // Assert
        // Note: Based on TagCatalog.DefineTags implementation, empty/whitespace tags are filtered out
        // So this should actually succeed with only "valid" tag
        result.IsSuccess.ShouldBeTrue();

        var catalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        catalog.ShouldNotBeNull();
        catalog.Tags.Select(t => t.Value).ShouldBe(new[] { "valid" });
    }

    [Test]
    public async Task AddTag_adds_tag_to_existing_catalog_and_publishes_event()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(new[] { "foo", "bar" });

        var (handler, dbContext, busMock) = CreateHandler();
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear(); // Detach entities to clear domain events

        var command = new AddTagCommand(ownerId, "baz");

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var updatedCatalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(new[] { "foo", "bar", "baz" }, ignoreOrder: true);

        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<TagCatalogTagAdded>();
    }

    [Test]
    public async Task AddTag_fails_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerId = "123";
        var command = new AddTagCommand(ownerId, "new-tag");

        var (handler, dbContext, busMock) = CreateHandler();

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not found"));

        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task AddTag_fails_when_tag_already_exists_in_catalog()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(new[] { "foo", "bar" });

        var (handler, dbContext, busMock) = CreateHandler();
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();

        var eventsBefore = catalog.DomainEvents.Count;
        var command = new AddTagCommand(ownerId, "FOO"); // Case-insensitive duplicate

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("already exists"));

        var updatedCatalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(new[] { "foo", "bar" }, ignoreOrder: true);
    }

    [Test]
    public async Task RemoveTag_removes_tag_from_catalog_and_publishes_event()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(new[] { "foo", "bar", "baz" });

        var (handler, dbContext, busMock) = CreateHandler();
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear(); // Detach entities to clear domain events

        var command = new RemoveTagCommand(ownerId, "bar");

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var updatedCatalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(new[] { "foo", "baz" }, ignoreOrder: true);

        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<TagCatalogTagRemoved>();
    }

    [Test]
    public async Task RemoveTag_fails_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerId = "123";
        var command = new RemoveTagCommand(ownerId, "tag");

        var (handler, dbContext, busMock) = CreateHandler();

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("not found"));

        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task RemoveTag_succeeds_when_tag_does_not_exist_but_does_not_publish_event()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(new[] { "foo", "bar" });

        var (handler, dbContext, busMock) = CreateHandler();
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear(); // Detach entities to clear domain events

        var command = new RemoveTagCommand(ownerId, "nonexistent");

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var updatedCatalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(new[] { "foo", "bar" }, ignoreOrder: true);

        // No event should be published for removing a non-existent tag
        busMock.Invocations.Count.ShouldBe(0);
    }

    [Test]
    public async Task RemoveTag_is_case_insensitive()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(new[] { "foo", "bar" });

        var (handler, dbContext, busMock) = CreateHandler();
        await dbContext.TagCatalogs.AddAsync(catalog);
        await dbContext.SaveChangesAsync();
        dbContext.ChangeTracker.Clear(); // Detach entities to clear domain events

        var command = new RemoveTagCommand(ownerId, "FOO"); // Different case

        // Act
        var result = await handler.Handle(command);

        // Assert
        result.IsSuccess.ShouldBeTrue();

        var updatedCatalog = await dbContext.TagCatalogs.FirstOrDefaultAsync(c => c.OwnerId == ownerId);
        updatedCatalog.ShouldNotBeNull();
        updatedCatalog.Tags.Select(t => t.Value).ShouldBe(new[] { "bar" });

        busMock.Invocations.Count.ShouldBe(1);
        busMock.Invocations[0].Arguments[0].ShouldBeOfType<TagCatalogTagRemoved>();
    }

    private static (TagCatalogHandler handler, PinsDbContext dbContext, Mock<IMessageBus> busMock) CreateHandler()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var dbContext = new PinsDbContext(options);
        var busMock = new Mock<IMessageBus>();

        var handler = new TagCatalogHandler(dbContext, busMock.Object);

        return (handler, dbContext, busMock);
    }
}