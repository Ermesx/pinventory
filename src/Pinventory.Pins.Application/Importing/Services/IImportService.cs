using FluentResults;

using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Application.Importing.Services;

public interface IImportService
{
    Task<Result<string>> InitiateAsync(Period? period = null, CancellationToken cancellationToken = default);

    Task<Result<(ImportState State, IEnumerable<Uri> Urls)>> CheckJobAsync(string archiveJobId,
        CancellationToken cancellationToken = default);

    Task<Result<Success>> CancelJobAsync(string archiveJobId, CancellationToken cancellationToken = default);
    Task DisposeDataArchivesAsync(CancellationToken cancellationToken = default);
}