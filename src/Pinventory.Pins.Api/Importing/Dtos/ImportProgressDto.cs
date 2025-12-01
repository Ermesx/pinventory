namespace Pinventory.Pins.Api.Importing.Dtos;

public sealed record ImportProgressDto(
    Guid ImportId,
    string ArchiveJobId,
    int Processed,
    int Created,
    int Updated,
    int Failed,
    int Conflicts);