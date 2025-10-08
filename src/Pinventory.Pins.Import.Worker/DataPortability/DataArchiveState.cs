namespace Pinventory.Pins.Import.Worker.DataPortability;

public record DataArchiveState(string State, IEnumerable<string> Urls);