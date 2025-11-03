using FluentResults;

namespace Pinventory.Pins.Application.Import.Services;

public interface IImportServiceFactory
{
    Task<Result<IImportService>> CreateAsync(string userId, CancellationToken cancellationToken = default);
}