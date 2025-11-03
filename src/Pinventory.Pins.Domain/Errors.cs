using FluentResults;

using Pinventory.Pins.Domain.Import;
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

        public static Error ImportAlreadyStartedOrFinished(ImportJobState state) => new($"Import already started or finished: {state}");

        public static Error ImportNotInProgress(ImportJobState state) => new($"Import is not in progress: {state}");

        public static Error ErrorMessageCannotBeEmpty() => new("Error message cannot be empty");

        public static Error BatchCountersMustBeNonNegative() => new("Batch counters must be non-negative");
    }
}