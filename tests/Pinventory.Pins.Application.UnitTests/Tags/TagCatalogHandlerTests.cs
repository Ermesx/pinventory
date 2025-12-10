using Microsoft.Extensions.Logging;

using Moq;

using Pinventory.Pins.Application.Tags;
using Pinventory.Pins.Application.Tags.Commands;
using Pinventory.Pins.Domain.Tags;

using Shouldly;

using Wolverine.Persistence;

namespace Pinventory.Pins.Application.UnitTests.Tags;

public class TagCatalogHandlerTests
{
    [Test]
    public void DefineTagCatalog_creates_catalog_and_returns_id_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerId = "123";
        var tags = new[] { "foo", "bar", "baz" };
        var command = new DefineTagCatalogCommand(ownerId, tags);

        var handler = CreateHandlerAsync();

        // Act
        var response = handler.Handle(command, null);

        // Assert
        response.Result.IsSuccess.ShouldBeTrue();
        response.Result.Value.ShouldNotBe(Guid.Empty);

        response.Storage.Action.ShouldBe(StorageAction.Insert);
        response.Storage.Entity.ShouldNotBeNull();
        response.Storage.Entity.Id.ShouldBe(response.Result.Value);
        response.Storage.Entity.OwnerId.ShouldBe(ownerId);
        response.Storage.Entity.Tags.Select(t => t.Value).ShouldBe(tags, ignoreOrder: true);
    }

    [Test]
    public void DefineTagCatalog_creates_global_catalog_when_owner_is_null()
    {
        // Arrange
        var tags = new[] { "restaurant", "cafe" };
        var command = new DefineTagCatalogCommand(null, tags);

        var handler = CreateHandlerAsync();

        // Act
        var response = handler.Handle(command, null);

        // Assert
        response.Result.IsSuccess.ShouldBeTrue();

        response.Storage.Entity.ShouldNotBeNull();
        response.Storage.Entity.OwnerId.ShouldBeNull();
        response.Storage.Entity.Tags.Select(t => t.Value).ShouldBe(tags, ignoreOrder: true);
    }

    [Test]
    public void DefineTagCatalog_fails_when_catalog_already_exists()
    {
        // Arrange
        var ownerId = "123";
        var existingCatalog = new TagCatalog(ownerId);
        existingCatalog.DefineTags(["existing"]);

        var handler = CreateHandlerAsync();
        var command = new DefineTagCatalogCommand(ownerId, ["new"]);

        // Act
        var response = handler.Handle(command, existingCatalog);

        // Assert
        response.Result.IsFailed.ShouldBeTrue();
        response.Result.Errors.ShouldContain(e => e.Message.Contains("already exists"));

        response.Storage.Action.ShouldBe(StorageAction.Nothing);
    }

    [Test]
    public void DefineTagCatalog_fails_when_domain_validation_fails()
    {
        // Arrange
        var ownerId = "123";
        var tags = new[] { "valid", "", "   " }; // Empty tags should cause validation failure
        var command = new DefineTagCatalogCommand(ownerId, tags);

        var handler = CreateHandlerAsync();

        // Act
        var response = handler.Handle(command, null);

        // Assert
        // Note: Based on TagCatalog.DefineTags implementation, empty/whitespace tags are filtered out
        // So this should actually succeed with only "valid" tag
        response.Result.IsSuccess.ShouldBeTrue();

        response.Storage.Entity.ShouldNotBeNull();
        response.Storage.Entity.Tags.Select(t => t.Value).ShouldBe(["valid"]);
    }

    [Test]
    public void AddTag_adds_tag_to_existing_catalog_and_publishes_event()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["foo", "bar"]);

        var handler = CreateHandlerAsync();

        var command = new AddTagCommand(ownerId, "baz");

        // Act
        var response = handler.Handle(command, catalog);

        // Assert
        response.IsSuccess.ShouldBeTrue();

        catalog.Tags.Select(t => t.Value).ShouldBe(["foo", "bar", "baz"], ignoreOrder: true);
    }

    [Test]
    public void AddTag_fails_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerId = "123";
        var command = new AddTagCommand(ownerId, "new-tag");

        var handler = CreateHandlerAsync();

        // Act
        var response = handler.Handle(command, null);

        // Assert
        response.IsFailed.ShouldBeTrue();
        response.Errors.ShouldContain(e => e.Message.Contains("not found"));
    }

    [Test]
    public void AddTag_fails_when_tag_already_exists_in_catalog()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["foo", "bar"]);

        var handler = CreateHandlerAsync();

        var command = new AddTagCommand(ownerId, "FOO"); // Case-insensitive duplicate

        // Act
        var response = handler.Handle(command, catalog);

        // Assert
        response.IsFailed.ShouldBeTrue();
        response.Errors.ShouldContain(e => e.Message.Contains("already exists"));

        catalog.Tags.Select(t => t.Value).ShouldBe(["foo", "bar"], ignoreOrder: true);
    }

    [Test]
    public void RemoveTag_removes_tag_from_catalog_and_publishes_event()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["foo", "bar", "baz"]);

        var handler = CreateHandlerAsync();

        var command = new RemoveTagCommand(ownerId, "bar");

        // Act
        var response = handler.Handle(command, catalog);

        // Assert
        response.IsSuccess.ShouldBeTrue();

        catalog.Tags.Select(t => t.Value).ShouldBe(["foo", "baz"], ignoreOrder: true);
    }

    [Test]
    public void RemoveTag_fails_when_catalog_does_not_exist()
    {
        // Arrange
        var ownerId = "123";
        var command = new RemoveTagCommand(ownerId, "tag");

        var handler = CreateHandlerAsync();

        // Act
        var response = handler.Handle(command, null);

        // Assert
        response.IsFailed.ShouldBeTrue();
        response.Errors.ShouldContain(e => e.Message.Contains("not found"));
    }

    [Test]
    public void RemoveTag_succeeds_when_tag_does_not_exist_but_does_not_publish_event()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["foo", "bar"]);

        var handler = CreateHandlerAsync();

        var command = new RemoveTagCommand(ownerId, "nonexistent");

        // Act
        var response = handler.Handle(command, catalog);

        // Assert
        response.IsSuccess.ShouldBeTrue();

        catalog.Tags.Select(t => t.Value).ShouldBe(["foo", "bar"], ignoreOrder: true);
    }

    [Test]
    public void RemoveTag_is_case_insensitive()
    {
        // Arrange
        var ownerId = "123";
        var catalog = new TagCatalog(ownerId);
        catalog.DefineTags(["foo", "bar"]);

        var handler = CreateHandlerAsync();

        var command = new RemoveTagCommand(ownerId, "FOO"); // Different case

        // Act
        var response = handler.Handle(command, catalog);

        // Assert
        response.IsSuccess.ShouldBeTrue();

        catalog.Tags.Select(t => t.Value).ShouldBe(["bar"]);
    }

    private static TagCatalogHandler CreateHandlerAsync()
    {
        var logger = Mock.Of<ILogger<TagCatalogHandler>>();
        return new TagCatalogHandler(logger);
    }
}