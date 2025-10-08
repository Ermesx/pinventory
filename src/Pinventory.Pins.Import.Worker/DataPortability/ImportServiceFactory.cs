using Microsoft.Extensions.Options;

using Pinventory.Google.Configuration;

namespace Pinventory.Pins.Import.Worker.DataPortability;

public sealed class ImportServiceFactory(IOptions<GoogleAuthOptions> options) : IImportServiceFactory
{
    public IImportService Create(ApiTokens tokens) => new ImportService(options, tokens);
}