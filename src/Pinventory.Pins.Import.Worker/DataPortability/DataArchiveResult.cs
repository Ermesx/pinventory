namespace Pinventory.Pins.Import.Worker.DataPortability;

public record DataArchiveResult(string State, IEnumerable<string> Urls);