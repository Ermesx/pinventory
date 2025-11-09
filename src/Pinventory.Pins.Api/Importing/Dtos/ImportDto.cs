using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Api.Importing.Dtos;

public record ImportDto(
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
    uint Total,
    IEnumerable<(string MapsUrl, DateTimeOffset AddedDate)> ConflictedPlaces,
    IEnumerable<(string MapsUrl, DateTimeOffset AddedDate)> FailedPlaces);