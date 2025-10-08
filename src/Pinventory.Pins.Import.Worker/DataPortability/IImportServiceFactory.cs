namespace Pinventory.Pins.Import.Worker.DataPortability;

public interface IImportServiceFactory
{
    IImportService Create(ApiTokens tokens);
}