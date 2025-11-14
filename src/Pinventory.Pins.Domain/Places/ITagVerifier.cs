namespace Pinventory.Pins.Domain.Places;

public interface ITagVerifier
{
    Task<bool> IsAllowedAsync(string? ownerId, string tag, CancellationToken cancellationToken = default);
}