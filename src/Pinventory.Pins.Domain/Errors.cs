using FluentResults;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Domain.Places;

namespace Pinventory.Pins.Domain;

public static class Errors
{
    public static class Pin
    {
        public static Error PinCannotBeOpened(PinStatus status) => new($"Pin cannot be opened from status {status}");

        public static Error PinCannotBeClosed(PinStatus status) => new($"Pin cannot be closed from status {status}");
    }

    public static class Tag
    {
        public static Error TagCannotBeEmpty() => new("Tag cannot be empty");

        public static Error TagAlreadyExists(string tag) => new($"Tag '{tag}' already exists");

        public static Error TagTooLong(int maxLength) => new($"Tag cannot be longer than {maxLength} characters");

        public static Error TagInvalidFormat() =>
            new("Tag can only contain letters, digits, spaces, hyphens and underscores");
    }

    public static class Location
    {
        public static Error InvalidLatitude(double latitude) =>
            new($"Latitude ({latitude}) must be between -90 and 90");

        public static Error InvalidLongitude(double longitude) =>
            new($"Longitude ({longitude}) must be between -180 and 180");
    }

    public static class Address
    {
        public static Error AddressLineCannotBeEmpty() => new("Address line cannot be empty");
    }

    public static class GooglePlaceId
    {
        public static Error GooglePlaceIdCannotBeEmpty() => new("Google place id cannot be empty");
    }

    public static class Import
    {
        public static Error ArchiveJobIdCannotBeEmpty() => new("Archive job id cannot be empty");

        public static Error ImportAlreadyStartedOrFinished(Importing.Import import) =>
            new($"Import {import.ArchiveJobId} already started or finished: {import.State} for user {import.UserId}");

        public static Error ImportNotInProgress(Importing.Import import) => new NotInProgressError(import);

        public static Error CannotRegisterPlaces(ImportState state) =>
            new($"Cannot register places because is not 'In Progress' (actual: '{state}')");

        public static Error PlacesCannotBeEmpty() => new EmptyPlacesError();

        public static Error ThereIsNoPlacesToProcess(Importing.Import import) =>
            new($"There is no places to process for import '{import.ArchiveJobId}' for user '{import.UserId}'");

        public static Error PlacesNotProcessed(Importing.Import import) =>
            new($"Not all places processed for import {import.ArchiveJobId} for user {import.UserId}");

        public class NotInProgressError(Importing.Import import)
            : Error($"Import {import.ArchiveJobId} is not in progress: {import.State} for user {import.UserId}");

        public class EmptyPlacesError() : Error("Places cannot be empty");
    }

    public static class Period
    {
        public static Error PeriodStartMustBeBeforeEnd(DateTimeOffset? start, DateTimeOffset? end) =>
            new IncorrectPeriodDates(start, end);

        public class IncorrectPeriodDates(DateTimeOffset? start, DateTimeOffset? end)
            : Error($"Period start ({start}) must be before end ({end})");
    }
}