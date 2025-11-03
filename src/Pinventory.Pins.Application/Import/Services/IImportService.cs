using Pinventory.Pins.Domain;
using Pinventory.Pins.Domain.Import;

namespace Pinventory.Pins.Application.Import.Services;

public interface IImportService
{
    Task<string> InitiateAsync(Period? period = null, CancellationToken cancellationToken = default);
    Task<(ImportJobState State, IEnumerable<Uri> Urls)> CheckJobAsync(string archiveJobId, CancellationToken cancellationToken = default);

    Task CancelJobAsync(string archiveJobId, CancellationToken cancellationToken = default);
    Task DisposeDataArchivesAsync(CancellationToken cancellationToken = default);
}