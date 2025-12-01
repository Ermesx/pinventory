using Pinventory.Pins.Api.Importing.Dtos;

namespace Pinventory.Pins.Api.Importing.Realtime;

public interface IImportProgressClient
{
    Task ProgressUpdated(ImportProgressDto dto);
    Task ImportCompleted(ImportCompletedDto dto);
    Task ImportFailed(ImportFailedDto dto);
}