using Pinventory.Pins.Domain.Places;
using Pinventory.Pins.Domain.Places.Events;
using Pinventory.Pins.Domain.UnitTests.TestUtils;

using Shouldly;

namespace Pinventory.Pins.Domain.UnitTests;

public class PinTests
{
    [Test]
    public void AssignTags_trims_deduplicates_filters_and_raises_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin();
        var tags = new[] { " foo  ", "foo", "bar", "bar", "", "   ", null, "bad" };
        var verifier = Tagging.CreateTagVerifier();

        // Act
        pin.AssignTags(tags!, verifier);

        // Assert
        pin.Tags.Select(t => t.Value).ShouldBe(["foo", "bar"], ignoreOrder: true);
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinTagsAssigned>();
        evt.AggregateId.ShouldBe(pin.Id);
        evt.Tags.ShouldBe(["foo", "bar"], ignoreOrder: true);
    }

    [Test]
    public void AssignTags_replaces_previous_tags()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin();
        var verifier = Tagging.CreateTagVerifier(["a", "b", "c"]);

        // Act
        pin.AssignTags(["a"], verifier);
        pin.AssignTags(["b", "c"], verifier);

        // Assert
        pin.Tags.Select(t => t.Value).ShouldBe(["b", "c"], ignoreOrder: true);
        pin.DomainEvents.Count.ShouldBe(2);
        pin.DomainEvents.Last().ShouldBeOfType<PinTagsAssigned>().Tags.ShouldBe(["b", "c"], ignoreOrder: true);
    }

    [Test]
    public void Close_from_Open_to_Closed_raises_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Open);

        // Act
        var result = pin.Close();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinClosed>();
        evt.AggregateId.ShouldBe(pin.Id);
        evt.Status.ShouldBe(PinStatus.Closed);
        evt.PreviousStatus.ShouldBe(PinStatus.Open);
    }

    [Test]
    public void Close_to_TemporaryClosed_raises_event_and_is_idempotent_for_same_status()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Open);

        // Act
        pin.Close(isTemporary: true).IsSuccess.ShouldBeTrue();
        pin.Close(isTemporary: true).IsSuccess.ShouldBeTrue(); // no new event expected

        // Assert
        pin.DomainEvents.OfType<PinClosed>().Count().ShouldBe(1);
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinClosed>();
        evt.Status.ShouldBe(PinStatus.TemporaryClosed);
        evt.PreviousStatus.ShouldBe(PinStatus.Open);
    }

    [Test]
    public void Close_on_Closed_returns_ok_without_new_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Open);
        pin.Close();
        var before = pin.DomainEvents.Count;

        // Act
        var res = pin.Close();

        // Assert
        res.IsSuccess.ShouldBeTrue();
        pin.DomainEvents.Count.ShouldBe(before); // no new event
    }

    [Test]
    public void Close_from_Unknown_fails()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Unknown);

        // Act
        var res = pin.Close();

        // Assert
        res.IsFailed.ShouldBeTrue();
        pin.DomainEvents.OfType<PinClosed>().ShouldBeEmpty();
    }

    [Test]
    public void Open_from_TemporaryClosed_to_Open_raises_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.TemporaryClosed);

        // Act
        var result = pin.Open();

        // Assert
        result.IsSuccess.ShouldBeTrue();
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinOpened>();
        evt.AggregateId.ShouldBe(pin.Id);
        evt.Status.ShouldBe(PinStatus.Open);
        evt.PreviousStatus.ShouldBe(PinStatus.TemporaryClosed);
    }

    [Test]
    public void Open_from_Unknown_to_Open_raises_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Unknown);

        // Act
        var res = pin.Open();

        // Assert
        res.IsSuccess.ShouldBeTrue();
        pin.DomainEvents.Last().ShouldBeOfType<PinOpened>().Status.ShouldBe(PinStatus.Open);
    }

    [Test]
    public void Open_when_already_Open_returns_ok_without_new_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Open);
        var before = pin.DomainEvents.Count;

        // Act
        var res = pin.Open();

        // Assert
        res.IsSuccess.ShouldBeTrue();
        pin.DomainEvents.Count.ShouldBe(before);
    }

    [Test]
    public void Open_from_Closed_fails()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Closed);

        // Act
        var res = pin.Open();

        // Assert
        res.IsFailed.ShouldBeTrue();
        pin.DomainEvents.OfType<PinOpened>().ShouldBeEmpty();
    }


    [Test]
    public void Open_from_Unknown_twice_is_idempotent()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Unknown);

        // Act
        var first = pin.Open();
        var second = pin.Open();

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        pin.DomainEvents.OfType<PinOpened>().Count().ShouldBe(1);
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinOpened>();
        evt.Status.ShouldBe(PinStatus.Open);
        evt.PreviousStatus.ShouldBe(PinStatus.Unknown);
    }

    [Test]
    public void Open_called_twice_when_already_Open_is_idempotent()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Open);

        // Act
        var first = pin.Open();
        var before = pin.DomainEvents.Count;
        var second = pin.Open();

        // Assert
        first.IsSuccess.ShouldBeTrue();
        second.IsSuccess.ShouldBeTrue();
        pin.DomainEvents.Count.ShouldBe(before);
        pin.DomainEvents.OfType<PinOpened>().ShouldBeEmpty();
    }

    [Test]
    public void Close_from_TemporaryClosed_to_Closed_raises_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.TemporaryClosed);

        // Act
        var res = pin.Close();

        // Assert
        res.IsSuccess.ShouldBeTrue();
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinClosed>();
        evt.Status.ShouldBe(PinStatus.Closed);
        evt.PreviousStatus.ShouldBe(PinStatus.TemporaryClosed);
    }

    [Test]
    public void AssignTags_is_case_insensitive_in_filtering_and_distinct()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin();
        var tags = new[] { " foo ", "FOO", "Foo", "BAR" };
        var verifier = Tagging.CreateTagVerifier();

        // Act
        pin.AssignTags(tags, verifier);

        // Assert
        pin.Tags.Select(t => t.Value).ShouldBe(["foo", "bar"], ignoreOrder: true);
        var evt = pin.DomainEvents.Last().ShouldBeOfType<PinTagsAssigned>();
        evt.Tags.ShouldBe(["foo", "bar"], ignoreOrder: true);
    }

    [Test]
    public void AssignTags_with_no_allowed_tags_clears_and_emits_no_event()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin();
        var verifier = Tagging.CreateTagVerifier(); // allowed: foo, bar
        pin.AssignTags(["foo"], verifier);
        var before = pin.DomainEvents.Count;

        // Act
        pin.AssignTags(["bad", "   ", null!]!, verifier);

        // Assert
        pin.Tags.ShouldBeEmpty();
        pin.DomainEvents.Count.ShouldBe(before);
    }

    [Test]
    public void Open_updates_StatusUpdatedAt_and_idempotent_call_does_not_change()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Unknown);
        var initial = pin.StatusUpdatedAt;

        // Act
        Thread.Sleep(5);
        var res1 = pin.Open();

        // Assert
        res1.IsSuccess.ShouldBeTrue();
        pin.StatusUpdatedAt.ShouldBeGreaterThan(initial);
        var afterOpen = pin.StatusUpdatedAt;
        var beforeCount = pin.DomainEvents.Count;

        // Act again (idempotent)
        Thread.Sleep(1);
        var res2 = pin.Open();

        // Assert again
        res2.IsSuccess.ShouldBeTrue();
        pin.StatusUpdatedAt.ShouldBe(afterOpen);
        pin.DomainEvents.Count.ShouldBe(beforeCount);
    }

    [Test]
    public void Close_updates_StatusUpdatedAt_and_idempotent_same_status_does_not_change()
    {
        // Arrange
        var pin = TestUtils.Pins.CreatePin(initial: PinStatus.Open);
        var initial = pin.StatusUpdatedAt;

        // Act
        Thread.Sleep(5);
        var res1 = pin.Close(isTemporary: true);

        // Assert
        res1.IsSuccess.ShouldBeTrue();
        pin.StatusUpdatedAt.ShouldBeGreaterThan(initial);
        var afterClose = pin.StatusUpdatedAt;
        var beforeCount = pin.DomainEvents.Count;

        // Act again (idempotent)
        Thread.Sleep(1);
        var res2 = pin.Close(isTemporary: true);

        // Assert again
        res2.IsSuccess.ShouldBeTrue();
        pin.StatusUpdatedAt.ShouldBe(afterClose);
        pin.DomainEvents.Count.ShouldBe(beforeCount);
    }
}