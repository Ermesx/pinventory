namespace Pinventory.Pins.Application.Import.Commands;

public record CancelImportCommand(string UserId, string ArchiveJobId);