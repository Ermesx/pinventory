namespace Pinventory.Pins.Api.Importing.Dtos;

public sealed record ImportFailedDto(Guid ImportId, string ArchiveJobId, string Error);