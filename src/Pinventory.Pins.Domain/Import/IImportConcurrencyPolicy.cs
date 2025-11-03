namespace Pinventory.Pins.Domain.Import;

/// <summary>
/// Domain policy for enforcing that at most one import runs at a time per user.
/// Implementation lives in application/infrastructure layer and should query the repository
/// for existing ImportJob entities with State == ImportJobState.InProgress for the given user.
/// </summary>
public interface IImportConcurrencyPolicy
{
    /// <summary>
    /// Returns true if no active import exists for the specified user.
    /// </summary>
    Task<bool> CanStartImportAsync(string userId, CancellationToken cancellationToken = default);
}