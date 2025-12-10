using FluentResults;

namespace Pinventory.Pins.Infrastructure;

public static class Errors
{
    public static class ImportProcess
    {
        public static Error ImportProcessTimeout(Guid importId) => new($"Import {importId} process timed out");
    }
}