using FluentResults;

using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Application.Importing.Services;

public interface IStarredPlacesProvider
{
    Task<Result<IReadOnlyList<StarredPlace>>> ProvideAsync(IReadOnlyList<Uri> urls, CancellationToken cancellationToken = default);
}