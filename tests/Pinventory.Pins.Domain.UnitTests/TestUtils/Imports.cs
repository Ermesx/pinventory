using Moq;

using Pinventory.Pins.Domain.Importing;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class Imports
{
    public static Import CreateStartedImport(string userId = "user123", string archiveJobId = "archive456")
    {
        var importJob = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(policy => policy.CanStartImportAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(true);
        importJob.StartAsync(archiveJobId, policyMock.Object).Wait();
        return importJob;
    }
}