namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportService
{
    Task<string> InitiateDataArchive(Period? period, CancellationToken cancellationToken = default);
    Task<DataArchiveResult> CheckDataArchive(string archiveJobId, CancellationToken cancellationToken = default);
}