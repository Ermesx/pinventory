using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;

using Pinventory.Identity.Tokens;
using Pinventory.Google.Token;

namespace Pinventory.Identity.UnitTests.Tokens;

public class TokenServiceTests
{
    [Test]
    public async Task GetGoogleTokensAsync_returns_null_when_user_not_found()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((User?)null);

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = new ClaimsPrincipal(); // no identity

        // Act
        var result = await sut.GetGoogleTokensAsync(principal);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetGoogleTokensAsync_returns_null_when_any_token_is_missing()
    {
        // Arrange
        var user = new User { Id = "test-user-id" };
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync((string?)null);

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = CreatePrincipal(user.Id);

        // Act
        var result = await sut.GetGoogleTokensAsync(principal);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetGoogleTokensAsync_returns_tokens_when_all_tokens_are_present()
    {
        // Arrange
        var user = new User { Id = "test-user-id" };
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "token_type"))
            .ReturnsAsync("Bearer");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "id_token"))
            .ReturnsAsync("test-id-token");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "access_token"))
            .ReturnsAsync("test-access-token");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "refresh_token"))
            .ReturnsAsync("test-refresh-token");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "expires_at"))
            .ReturnsAsync("2025-12-31T23:59:59Z");

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = CreatePrincipal(user.Id);

        // Act
        var result = await sut.GetGoogleTokensAsync(principal);

        // Assert
        result.ShouldNotBeNull();
        result.IdToken.Token.ShouldBe("test-id-token");
        result.IdToken.TokenType.ShouldBe("Bearer");
        result.AccessToken.Token.ShouldBe("test-access-token");
        result.AccessToken.TokenType.ShouldBe("Bearer");
        result.AccessToken.ExpiresAt.ShouldBe(DateTimeOffset.Parse("2025-12-31T23:59:59Z"));
        result.RefreshToken.Token.ShouldBe("test-refresh-token");
        result.RefreshToken.TokenType.ShouldBe("Bearer");
    }

    [Test]
    public async Task RefreshGoogleAccessTokenAsync_returns_null_when_user_not_found()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((User?)null);

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = new ClaimsPrincipal();

        // Act
        var result = await sut.RefreshGoogleAccessTokenAsync(principal, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task RefreshGoogleAccessTokenAsync_returns_null_when_refresh_token_is_missing()
    {
        // Arrange
        var user = new User { Id = "test-user-id" };
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "refresh_token"))
            .ReturnsAsync((string?)null);

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = CreatePrincipal(user.Id);

        // Act
        var result = await sut.RefreshGoogleAccessTokenAsync(principal, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task RefreshGoogleAccessTokenAsync_returns_null_when_token_endpoint_returns_null()
    {
        // Arrange
        var user = new User { Id = "test-user-id" };
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "refresh_token"))
            .ReturnsAsync("test-refresh-token");

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        tokenEndpointMock
            .Setup(x => x.RefreshTokenAsync("test-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TokenResponse?)null);

        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = CreatePrincipal(user.Id);

        // Act
        var result = await sut.RefreshGoogleAccessTokenAsync(principal, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task RefreshGoogleAccessTokenAsync_returns_new_token_and_updates_user_tokens()
    {
        // Arrange
        var user = new User { Id = "test-user-id" };
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "refresh_token"))
            .ReturnsAsync("test-refresh-token");

        userManagerMock
            .Setup(x => x.SetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, It.IsAny<string>(), It.IsAny<string?>()))
            .ReturnsAsync(IdentityResult.Success);

        var tokenResponse = new TokenResponse(
            AccessToken: "new-access-token",
            ExpiresIn: 3600,
            TokenType: "Bearer",
            Scope: "email profile"
        );

        var tokenEndpointMock = new Mock<IGoogleTokenEndpoint>();
        tokenEndpointMock
            .Setup(x => x.RefreshTokenAsync("test-refresh-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(tokenResponse);

        var sut = new TokenService(userManagerMock.Object, tokenEndpointMock.Object);
        var principal = CreatePrincipal(user.Id);

        var beforeRefresh = DateTimeOffset.UtcNow;

        // Act
        var result = await sut.RefreshGoogleAccessTokenAsync(principal, CancellationToken.None);

        var afterRefresh = DateTimeOffset.UtcNow;

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe("new-access-token");
        result.TokenType.ShouldBe("Bearer");
        result.ExpiresAt.ShouldBeGreaterThanOrEqualTo(beforeRefresh.AddSeconds(3600));
        result.ExpiresAt.ShouldBeLessThanOrEqualTo(afterRefresh.AddSeconds(3600));

        // Verify tokens were updated in user manager
        userManagerMock.Verify(
            x => x.SetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "access_token", "new-access-token"),
            Times.Once);

        userManagerMock.Verify(
            x => x.SetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "token_type", "Bearer"),
            Times.Once);

        userManagerMock.Verify(
            x => x.SetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "expires_at", It.IsAny<string>()),
            Times.Once);
    }

    private static ClaimsPrincipal CreatePrincipal(string userId) =>
        new(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)]));

    private static Mock<UserManager<User>> CreateUserManagerMock()
    {
        var store = Mock.Of<IUserStore<User>>();
        var options = Options.Create(new IdentityOptions());
        var passwordHasher = new PasswordHasher<User>();
        var userValidators = Array.Empty<IUserValidator<User>>();
        var passwordValidators = Array.Empty<IPasswordValidator<User>>();
        var keyNormalizer = new UpperInvariantLookupNormalizer();
        var errors = new IdentityErrorDescriber();
        var services = new ServiceCollection().BuildServiceProvider();
        var logger = Mock.Of<ILogger<UserManager<User>>>();

        return new Mock<UserManager<User>>(
            store,
            options,
            passwordHasher,
            userValidators,
            passwordValidators,
            keyNormalizer,
            errors,
            services,
            logger
            );
    }
}

