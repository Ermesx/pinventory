namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportServiceFactory
{
    Task<IImportService> Create(string userId, CancellationToken cancellationToken = default);
}