using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure.ReadModels;

namespace Pinventory.Pins.Api.Importing.Dtos;

public sealed record ImportDto(
    Guid Id,
    string? ArchiveJobId,
    ImportState State,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    int Processed,
    int Created,
    int Updated,
    int Failed,
    int Conflicts,
    int Total)
{
    public static ImportDto From(ImportSummary import) => new(
        import.Id,
        import.ArchiveJobId,
        import.State,
        import.StartedAt,
        import.CompletedAt,
        import.Processed,
        import.Created,
        import.Updated,
        import.Failed,
        import.Conflicts,
        import.Total);
}