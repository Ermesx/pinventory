using Moq;

using Pinventory.Pins.Domain.Import;

namespace Pinventory.Pins.Domain.UnitTests.TestUtils;

public static class ImportJobs
{
    public static ImportJob CreateStartedImportJob(string userId = "user123", string archiveJobId = "archive456")
    {
        var importJob = new ImportJob(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(policy => policy.CanStartImportAsync(It.IsAny<string>(), CancellationToken.None)).ReturnsAsync(true);
        importJob.StartAsync(archiveJobId, policyMock.Object).Wait();
        return importJob;
    }
}