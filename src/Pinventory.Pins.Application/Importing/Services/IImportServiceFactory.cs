using FluentResults;

namespace Pinventory.Pins.Application.Importing.Services;

public interface IImportServiceFactory
{
    Task<Result<IImportService>> CreateAsync(string userId, CancellationToken cancellationToken = default);
}