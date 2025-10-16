using System.Security.Claims;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Pinventory.Google.Tokens;
using Pinventory.Identity.Tokens;

using Shouldly;

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

        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var sut = new TokenService(userManagerMock.Object);
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

        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var sut = new TokenService(userManagerMock.Object);
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

        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var sut = new TokenService(userManagerMock.Object);
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
        result.AccessToken.RefreshToken.Token.ShouldBe("test-refresh-token");
        result.AccessToken.RefreshToken.TokenType.ShouldBe("Bearer");
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

    private static Mock<SignInManager<User>> CreateSignInManagerMock(UserManager<User> userManager)
    {
        var contextAccessor = Mock.Of<IHttpContextAccessor>();
        var userPrincipalFactory = Mock.Of<IUserClaimsPrincipalFactory<User>>();
        var options = Options.Create(new IdentityOptions());
        var logger = Mock.Of<ILogger<SignInManager<User>>>();
        var schemes = Mock.Of<IAuthenticationSchemeProvider>();
        var confirmation = Mock.Of<IUserConfirmation<User>>();
        return new Mock<SignInManager<User>>(userManager, contextAccessor, userPrincipalFactory, options, logger, schemes, confirmation);
    }

    [Test]
    public async Task SaveGoogleDataPortabilityTokensAsync_returns_failure_when_user_not_found()
    {
        // Arrange
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync((User?)null);

        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        var sut = new TokenService(userManagerMock.Object);

        // Act
        var res = await sut.SaveGoogleDataPortabilityTokensAsync(new ClaimsPrincipal(),
            GoogleAccessToken.Create("acc", "Bearer", "ref", DateTimeOffset.UtcNow));

        // Assert
        res.IsFailed.ShouldBeTrue();
    }

    [Test]
    public async Task SaveGoogleDataPortabilityTokensAsync_saves_all_tokens_under_dataportability_provider()
    {
        // Arrange
        var user = new User { Id = "u1" };
        var userManagerMock = CreateUserManagerMock();
        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        var signInManagerMock = CreateSignInManagerMock(userManagerMock.Object);
        userManagerMock
            .Setup(x => x.SetAuthenticationTokenAsync(user, "Google.DataPortability", It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var sut = new TokenService(userManagerMock.Object);
        var now = DateTimeOffset.Parse("2025-01-02T03:04:05Z");

        // Act
        var res = await sut.SaveGoogleDataPortabilityTokensAsync(CreatePrincipal(user.Id),
            GoogleAccessToken.Create("acc", "Bearer", "ref", now));

        // Assert
        res.IsSuccess.ShouldBeTrue();
        userManagerMock.Verify(x => x.SetAuthenticationTokenAsync(user, "Google.DataPortability", "access_token", "acc"), Times.Once);
        userManagerMock.Verify(x => x.SetAuthenticationTokenAsync(user, "Google.DataPortability", "refresh_token", "ref"), Times.Once);
        userManagerMock.Verify(x => x.SetAuthenticationTokenAsync(user, "Google.DataPortability", "token_type", "Bearer"), Times.Once);
        userManagerMock.Verify(x => x.SetAuthenticationTokenAsync(user, "Google.DataPortability", "expires_at", now.ToString("o")),
            Times.Once);
    }

    [Test]
    public async Task GetGoogleTokensAsync_returns_distinct_access_tokens_for_google_and_dataportability()
    {
        // Arrange
        var user = new User { Id = "user-1" };
        var userManagerMock = CreateUserManagerMock();

        userManagerMock
            .Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);

        // Standard Google tokens
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "token_type"))
            .ReturnsAsync("Bearer");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "id_token"))
            .ReturnsAsync("id-token");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "access_token"))
            .ReturnsAsync("google-access-token-123");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "refresh_token"))
            .ReturnsAsync("google-refresh");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, GoogleDefaults.AuthenticationScheme, "expires_at"))
            .ReturnsAsync("2026-01-01T00:00:00Z");

        // Data Portability tokens (distinct)
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, "Google.DataPortability", "access_token"))
            .ReturnsAsync("dp-access-token-456");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, "Google.DataPortability", "refresh_token"))
            .ReturnsAsync("dp-refresh");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, "Google.DataPortability", "token_type"))
            .ReturnsAsync("Bearer");
        userManagerMock
            .Setup(x => x.GetAuthenticationTokenAsync(user, "Google.DataPortability", "expires_at"))
            .ReturnsAsync("2026-01-02T00:00:00Z");

        var sut = new TokenService(userManagerMock.Object);
        var principal = CreatePrincipal(user.Id);

        // Act
        var tokens = await sut.GetGoogleTokensAsync(principal);

        // Assert
        tokens.ShouldNotBeNull();
        tokens!.DataPortabilityAccessToken.ShouldNotBeNull();
        tokens.AccessToken.Token.ShouldNotBe(tokens.DataPortabilityAccessToken!.Token);
    }

}