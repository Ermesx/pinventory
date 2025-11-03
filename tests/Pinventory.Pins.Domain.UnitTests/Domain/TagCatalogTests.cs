using Pinventory.Pins.Domain.Tags;
using Pinventory.Pins.Domain.Tags.Events;

using Shouldly;

namespace Pinventory.Pins.Domain.UnitTests.Domain;

public class TagCatalogTests
{
    [Test]
    public void DefineTags_trims_deduplicates_and_raises_event_with_normalized_tags()
    {
        // Arrange
        var catalog = new TagCatalog();
        var tags = new[] { " foo  ", "foo", "BAR", "bar", "", "   ", string.Empty }!;

        // Act
        var result = catalog.DefineTags(tags);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        catalog.Tags.Select(t => t.Value).ShouldBe(["foo", "bar"], ignoreOrder: true);
        var evt = catalog.DomainEvents.Last().ShouldBeOfType<TagCatalogTagsDefined>();
        evt.AggregateId.ShouldBe(catalog.Id);
        evt.Tags.ShouldBe(["foo", "bar"], ignoreOrder: true);
    }

    [Test]
    public void DefineTags_replaces_previous_tags_and_raises_event_each_time()
    {
        // Arrange
        var catalog = new TagCatalog();

        // Act
        catalog.DefineTags(["a"]);
        catalog.DefineTags(["b", "c"]);

        // Assert
        catalog.Tags.Select(t => t.Value).ShouldBe(["b", "c"], ignoreOrder: true);
        catalog.DomainEvents.Count.ShouldBe(2);
        catalog.DomainEvents.Last().ShouldBeOfType<TagCatalogTagsDefined>().Tags.ShouldBe(["b", "c"], ignoreOrder: true);
    }

    [Test]
    public void AddTag_adds_when_missing_and_raises_event_duplicate_is_failure()
    {
        // Arrange
        var catalog = new TagCatalog();

        // Act
        var added = catalog.AddTag("foo");
        var duplicate = catalog.AddTag("FOO");

        // Assert
        added.IsSuccess.ShouldBeTrue();
        catalog.Tags.Select(t => t.Value).ShouldBe(["foo"]);
        catalog.DomainEvents.Last().ShouldBeOfType<TagCatalogTagAdded>().Tag.ShouldBe("foo");

        duplicate.IsSuccess.ShouldBeFalse();
        catalog.DomainEvents.Count.ShouldBe(1); // no new event on duplicate
    }

    [Test]
    public void RemoveTag_removes_when_present_and_raises_event_missing_is_idempotent()
    {
        // Arrange
        var catalog = new TagCatalog();
        catalog.DefineTags(["foo"]);
        var eventsBefore = catalog.DomainEvents.Count;

        // Act
        var removed = catalog.RemoveTag("Foo");
        var missing = catalog.RemoveTag("bar");

        // Assert
        removed.IsSuccess.ShouldBeTrue();
        catalog.Tags.ShouldBeEmpty();
        catalog.DomainEvents.Count.ShouldBe(eventsBefore + 1);
        catalog.DomainEvents.Last().ShouldBeOfType<TagCatalogTagRemoved>().Tag.ShouldBe("Foo");

        missing.IsSuccess.ShouldBeTrue();
        catalog.DomainEvents.Count.ShouldBe(eventsBefore + 1); // no new event
    }

    [Test]
    public void OwnerId_is_preserved_when_provided()
    {
        // Arrange
        var ownerId = "123";

        // Act
        var catalog = new TagCatalog(ownerId);

        // Assert
        catalog.OwnerId.ShouldBe(ownerId);
    }
}