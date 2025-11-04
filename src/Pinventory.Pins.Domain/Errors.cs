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
    }

    public static class ImportJob
    {
        public static Error ArchiveJobIdCannotBeEmpty() => new("Archive job id cannot be empty");

        public static Error ImportAlreadyStartedOrFinished(Import import) =>
            new($"Import {import.ArchiveJobId} already started or finished: {import.State} for user {import.UserId}");

        public static Error ImportNotInProgress(Import import) =>
            new($"Import {import.ArchiveJobId} is not in progress: {import.State} for user {import.UserId}");

        public static Error ErrorMessageCannotBeEmpty() => new("Error message cannot be empty");

        public static Error BatchCountersMustBeNonNegative() => new("Batch counters must be non-negative");

        public static Error ImportNotCompleteYet(Import import) =>
            new(
                $"Import {import.ArchiveJobId} is not complete ({import.Processed} of {import.Total} processed) yet for user {import.UserId}");
    }
}