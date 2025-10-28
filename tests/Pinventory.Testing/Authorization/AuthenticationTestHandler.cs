using System.Security.Claims;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Pinventory.Testing.Authorization;

public class AuthenticationTestHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ISystemClock clock,
    CurrentUserIdProvider currentUserIdProvider)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder, clock)
{
    public const string AuthenticationScheme = "TestScheme";
    public const string TestUserId = "test-user-id";
    public const string AdminUserId = "admin-user-id";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var userId = currentUserIdProvider.CurrentUserId;

        var claims = new[]
        {
            new Claim(ClaimTypes.Name, "Test User"), new Claim(ClaimTypes.NameIdentifier, userId), new Claim("sub", userId)
        };

        var identity = new ClaimsIdentity(claims, AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, AuthenticationScheme);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}