namespace Pinventory.Pins.Api.Importing.Dtos;

public sealed record StartImportDto(DateTimeOffset? Start, DateTimeOffset? End);