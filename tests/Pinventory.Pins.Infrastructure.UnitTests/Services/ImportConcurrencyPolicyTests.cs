using FluentResults;

using Microsoft.EntityFrameworkCore;

using Moq;

using Nager.Country;

using Pinventory.Pins.Domain.Importing;
using Pinventory.Pins.Infrastructure.Services;

using Shouldly;

namespace Pinventory.Pins.Infrastructure.UnitTests.Services;

public class ImportConcurrencyPolicyTests
{
    [Test]
    public async Task CanStartImportAsync_returns_true_when_no_imports_exist_for_user()
    {
        // Arrange
        var userId = "user-1";
        var (policy, _) = await CreatePolicyAsync();

        // Act
        var result = await policy.CanStartImportAsync(userId);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task CanStartImportAsync_returns_true_when_no_in_progress_imports_exist_for_user()
    {
        // Arrange
        var userId = "user-1";
        var (policy, dbContext) = await CreatePolicyAsync();

        var completedImport = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await completedImport.StartAsync("job-1", policyMock.Object);
        completedImport.RegisterBatch([
            new StarredPlace("Place", "http://maps.google.com/?cid=123", "Address", Alpha2Code.PL, 1.0, 2.0, DateTimeOffset.UtcNow, null)
        ]);
        var batchId = completedImport.BatchesMap.Keys.First();
        var validatorMock = new Mock<IStaredPlaceValidator>();
        validatorMock.Setup(v => v.ValidateAsync(It.IsAny<Import>(), It.IsAny<StarredPlace>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(StarredPlaceState.New);
        await completedImport.ProcessBatchAsync(batchId, validatorMock.Object);
        completedImport.TryComplete();

        await dbContext.Imports.AddAsync(completedImport);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await policy.CanStartImportAsync(userId);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task CanStartImportAsync_returns_false_when_in_progress_import_exists_for_user()
    {
        // Arrange
        var userId = "user-1";
        var (policy, dbContext) = await CreatePolicyAsync();

        var inProgressImport = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await inProgressImport.StartAsync("job-1", policyMock.Object);

        await dbContext.Imports.AddAsync(inProgressImport);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await policy.CanStartImportAsync(userId);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task CanStartImportAsync_returns_true_when_in_progress_import_exists_for_different_user()
    {
        // Arrange
        var userId1 = "user-1";
        var userId2 = "user-2";
        var (policy, dbContext) = await CreatePolicyAsync();

        var inProgressImport = new Import(userId1);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await inProgressImport.StartAsync("job-1", policyMock.Object);

        await dbContext.Imports.AddAsync(inProgressImport);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await policy.CanStartImportAsync(userId2);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task CanStartImportAsync_returns_true_when_failed_import_exists_for_user()
    {
        // Arrange
        var userId = "user-1";
        var (policy, dbContext) = await CreatePolicyAsync();

        var failedImport = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await failedImport.StartAsync("job-1", policyMock.Object);
        failedImport.Fail(new Error("Test error"));

        await dbContext.Imports.AddAsync(failedImport);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await policy.CanStartImportAsync(userId);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task CanStartImportAsync_returns_true_when_cancelled_import_exists_for_user()
    {
        // Arrange
        var userId = "user-1";
        var (policy, dbContext) = await CreatePolicyAsync();

        var cancelledImport = new Import(userId);
        var policyMock = new Mock<IImportConcurrencyPolicy>();
        policyMock.Setup(p => p.CanStartImportAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        await cancelledImport.StartAsync("job-1", policyMock.Object);
        cancelledImport.Cancel();

        await dbContext.Imports.AddAsync(cancelledImport);
        await dbContext.SaveChangesAsync();

        // Act
        var result = await policy.CanStartImportAsync(userId);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task CanStartImportAsync_respects_cancellation_token()
    {
        // Arrange
        var userId = "user-1";
        var (policy, _) = await CreatePolicyAsync();
        var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert
        await Should.ThrowAsync<OperationCanceledException>(async () =>
            await policy.CanStartImportAsync(userId, cts.Token));
    }

    private static async Task<(ImportConcurrencyPolicy policy, PinsDbContext dbContext)> CreatePolicyAsync()
    {
        var options = new DbContextOptionsBuilder<PinsDbContext>()
            .UseSqlite(connectionString: "Data Source=:memory:")
            .Options;

        var dbContext = new PinsDbContext(options);
        await dbContext.Database.OpenConnectionAsync();
        await dbContext.Database.EnsureCreatedAsync();

        var policy = new ImportConcurrencyPolicy(dbContext);

        return (policy, dbContext);
    }
}