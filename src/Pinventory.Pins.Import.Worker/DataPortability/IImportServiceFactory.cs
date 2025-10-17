using FluentResults;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportServiceFactory
{
    Task<Result<IImportService>> CreateAsync(string userId, CancellationToken cancellationToken = default);
    
    void Destroy(string userId);
}