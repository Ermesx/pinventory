using Moq;

using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class Imports
{
    public static async Task<Import> CreateStartedImport(string userId = "user123", string archiveJobId = "archive456")
    {
        var import = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(policy => policy.CanStartImportAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(true);
        await import.StartAsync(archiveJobId, policyMock.Object);
        return import;
    }
}