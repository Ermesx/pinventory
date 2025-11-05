using FluentResults;

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
    }

    public static class Import
    {
        public static Error ArchiveJobIdCannotBeEmpty() => new("Archive job id cannot be empty");

        public static Error ImportAlreadyStartedOrFinished(Importing.Import import) =>
            new($"Import {import.ArchiveJobId} already started or finished: {import.State} for user {import.UserId}");

        public static Error ImportNotInProgress(Importing.Import import) => new NotInProgressError(import);

        public static Error ErrorMessageCannotBeEmpty() => new("Error message cannot be empty");

        public static Error BatchCountersMustBeNonNegative() => new("Batch counters must be non-negative");

        public static Error ImportNotCompleteYet(Importing.Import import) =>
            new(
                $"Import {import.ArchiveJobId} is not complete ({import.Processed} of {import.Total} processed) yet for user {import.UserId}");

        public class NotInProgressError(Importing.Import import)
            : Error($"Import {import.ArchiveJobId} is not in progress: {import.State} for user {import.UserId}");
    }
}