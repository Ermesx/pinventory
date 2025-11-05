namespace Pinventory.Pins.Domain.Importing;

public interface IImportConcurrencyPolicy
{
    Task<bool> CanStartImportAsync(string userId, CancellationToken cancellationToken = default);
}