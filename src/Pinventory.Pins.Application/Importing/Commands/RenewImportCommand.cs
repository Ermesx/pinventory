namespace Pinventory.Pins.Application.Importing.Commands;

// TODO: Switch from ArchiveJobId to ImportId as Id to manage Imports ans Sagas directly
public record RenewImportCommand(string UserId, string ArchiveJobId);