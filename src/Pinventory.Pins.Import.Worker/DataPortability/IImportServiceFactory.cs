using FluentResults;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportServiceFactory
{
    Task<Result<IImportService>> Create(string userId, CancellationToken cancellationToken = default);
}