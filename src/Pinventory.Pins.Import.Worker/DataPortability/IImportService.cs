namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportService
{
    DateTimeOffset LastUsed { get; }
    
    Task<string> InitiateDataArchiveAsync(Period? period = null, CancellationToken cancellationToken = default);
    Task<DataArchiveResult> CheckDataArchiveAsync(string archiveJobId, CancellationToken cancellationToken = default);
}