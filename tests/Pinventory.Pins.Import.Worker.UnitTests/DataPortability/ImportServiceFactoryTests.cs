using Google.Protobuf.WellKnownTypes;

using Grpc.Core;

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Pinventory.Google.Configuration;
using Pinventory.Identity.Tokens.Grpc;
using Pinventory.Pins.Import.Worker.DataPortability;

using Shouldly;

namespace Pinventory.Pins.Import.Worker.UnitTests.DataPortability;

public class ImportServiceFactoryTests
{
    [Test]
    public async Task CreateAsync_returns_cached_instance_on_subsequent_calls()
    {
        // Arrange
        var (factory, clientMock, cache) = CreateFactory();

        var token = new PairToken
        {
            Token = "access",
            TokenType = "Bearer",
            RefreshToken = "refresh",
            ExpiresAt = Timestamp.FromDateTimeOffset(DateTimeOffset.UtcNow.AddHours(1))
        };

        clientMock
            .Setup(c => c.GetAccessTokenAsync(It.IsAny<UserRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<TokenResponse>(
                Task.FromResult(new TokenResponse { DataPortabilityAccessToken = token }),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        // Act
        var result1 = await factory.CreateAsync("user-1", CancellationToken.None);
        var result2 = await factory.CreateAsync("user-1", CancellationToken.None);

        // Assert
        result1.IsSuccess.ShouldBeTrue();
        result2.IsSuccess.ShouldBeTrue();
        ReferenceEquals(result1.Value, result2.Value).ShouldBeTrue();
    }

    [Test]
    public async Task CreateAsync_returns_failure_when_no_tokens_found()
    {
        // Arrange
        var (factory, clientMock, _) = CreateFactory();

        clientMock
            .Setup(c => c.GetAccessTokenAsync(It.IsAny<UserRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<TokenResponse>(
                Task.FromException<TokenResponse>(new RpcException(new Status(StatusCode.NotFound, "not found"))),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        // Act
        var result = await factory.CreateAsync("user-1", CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("No tokens found for user"));
    }

    [Test]
    public async Task CreateAsync_returns_failure_when_data_portability_token_missing()
    {
        // Arrange
        var (factory, clientMock, _) = CreateFactory();

        clientMock
            .Setup(c => c.GetAccessTokenAsync(It.IsAny<UserRequest>(), null, null, It.IsAny<CancellationToken>()))
            .Returns(new AsyncUnaryCall<TokenResponse>(
                Task.FromResult(new TokenResponse()),
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { }));

        // Act
        var result = await factory.CreateAsync("user-1", CancellationToken.None);

        // Assert
        result.IsFailed.ShouldBeTrue();
        result.Errors.ShouldContain(e => e.Message.Contains("Data portability token is missing"));
    }

    private static (ImportServiceFactory factory, Mock<Tokens.TokensClient> clientMock, IMemoryCache cache) CreateFactory()
    {
        var options = Options.Create(new GoogleAuthOptions { ClientId = "client-id", ClientSecret = "client-secret" });

        var clientMock = new Mock<Tokens.TokensClient>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var loggerFactory = new LoggerFactory();
        var logger = loggerFactory.CreateLogger<ImportServiceFactory>();

        var factory = new ImportServiceFactory(options, clientMock.Object, cache, logger, loggerFactory);

        return (factory, clientMock, cache);
    }
}