using Shouldly;

namespace Pinventory.Pins.Domain.UnitTests;

public class PeriodTests
{
    private static readonly DateTimeOffset FixedEndDate = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);

    [Test]
    public void create_clamps_start_to_unix_epoch_when_start_is_before_unix_epoch()
    {
        // Arrange
        var startBeforeEpoch = DateTimeOffset.UnixEpoch.AddDays(-1);
        var validEnd = FixedEndDate;

        // Act
        var result = Period.Create(startBeforeEpoch, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Value.End.ShouldBe(validEnd);
    }

    [Test]
    public void Create_clamps_start_to_unix_epoch_when_start_is_very_early()
    {
        // Arrange
        var veryEarlyStart = DateTimeOffset.MinValue;
        var validEnd = FixedEndDate;

        // Act
        var result = Period.Create(veryEarlyStart, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Value.End.ShouldBe(validEnd);
    }

    [Test]
    public void Create_returns_period_start_must_be_before_end_error_when_clamped_start_equals_end()
    {
        // Arrange
        var startBeforeEpoch = DateTimeOffset.UnixEpoch.AddDays(-1);
        var endAtEpoch = DateTimeOffset.UnixEpoch;

        // Act
        var result = Period.Create(startBeforeEpoch, endAtEpoch);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is Errors.Period.IncorrectPeriodDates);
    }

    [Test]
    public void create_returns_period_start_must_be_before_end_error_when_clamped_start_is_after_end()
    {
        // Arrange
        var veryEarlyStart = DateTimeOffset.MinValue;
        var endBeforeEpoch = DateTimeOffset.UnixEpoch.AddDays(-1);

        // Act
        var result = Period.Create(veryEarlyStart, endBeforeEpoch);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e is Errors.Period.IncorrectPeriodDates);
    }

    [Test]
    public void create_returns_valid_period_when_start_is_exactly_unix_epoch()
    {
        // Arrange
        var startAtEpoch = DateTimeOffset.UnixEpoch;
        var validEnd = FixedEndDate;

        // Act
        var result = Period.Create(startAtEpoch, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Value.End.ShouldBe(validEnd);
    }

    [Test]
    public void create_returns_valid_period_when_start_is_after_unix_epoch()
    {
        // Arrange
        var startAfterEpoch = DateTimeOffset.UnixEpoch.AddDays(1);
        var validEnd = FixedEndDate;

        // Act
        var result = Period.Create(startAfterEpoch, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(startAfterEpoch);
        result.Value.End.ShouldBe(validEnd);
    }
}
