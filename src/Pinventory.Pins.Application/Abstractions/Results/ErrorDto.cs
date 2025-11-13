namespace Pinventory.Pins.Application.Abstractions.Results;

public record ErrorDto(string Message, IEnumerable<string> Reasons, string? Type);