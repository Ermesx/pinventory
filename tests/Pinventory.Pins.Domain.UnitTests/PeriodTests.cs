using Shouldly;

namespace Pinventory.Pins.Domain.UnitTests;

public class PeriodTests
{
    [Test]
    public void Create_ClampsStartToUnixEpoch_WhenStartIsBeforeUnixEpoch()
    {
        // Arrange
        var startBeforeEpoch = DateTimeOffset.UnixEpoch.AddDays(-1);
        var validEnd = DateTimeOffset.UtcNow;

        // Act
        var result = Period.Create(startBeforeEpoch, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Value.End.ShouldBe(validEnd);
    }

    [Test]
    public void Create_ClampsStartToUnixEpoch_WhenStartIsVeryEarly()
    {
        // Arrange
        var veryEarlyStart = DateTimeOffset.MinValue;
        var validEnd = DateTimeOffset.UtcNow;

        // Act
        var result = Period.Create(veryEarlyStart, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Value.End.ShouldBe(validEnd);
    }

    [Test]
    public void Create_ReturnsPeriodStartMustBeBeforeEndError_WhenClampedStartEqualsEnd()
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
    public void Create_ReturnsPeriodStartMustBeBeforeEndError_WhenClampedStartIsAfterEnd()
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
    public void Create_ReturnsValidPeriod_WhenStartIsExactlyUnixEpoch()
    {
        // Arrange
        var startAtEpoch = DateTimeOffset.UnixEpoch;
        var validEnd = DateTimeOffset.UtcNow;

        // Act
        var result = Period.Create(startAtEpoch, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(DateTimeOffset.UnixEpoch);
        result.Value.End.ShouldBe(validEnd);
    }

    [Test]
    public void Create_ReturnsValidPeriod_WhenStartIsAfterUnixEpoch()
    {
        // Arrange
        var startAfterEpoch = DateTimeOffset.UnixEpoch.AddDays(1);
        var validEnd = DateTimeOffset.UtcNow;

        // Act
        var result = Period.Create(startAfterEpoch, validEnd);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Start.ShouldBe(startAfterEpoch);
        result.Value.End.ShouldBe(validEnd);
    }
}
