namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportService
{
    Task<string> InitiateDataArchive(Period? period);
    Task<DataArchiveState> CheckDataArchive(string archiveJobId);
}