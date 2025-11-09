namespace Pinventory.Pins.Api.Importing.Dtos;

public sealed record ImportCompletedDto(Guid ImportId, string ArchiveJobId);