namespace Pinventory.Pins.Application.Results;

public record ErrorDto(string Message, IEnumerable<string> Reasons, string? Type);