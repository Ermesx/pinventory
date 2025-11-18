namespace Pinventory.Pins.Application.Importing.Commands;

public record RenewImportCommand(string UserId, string ArchiveJobId);