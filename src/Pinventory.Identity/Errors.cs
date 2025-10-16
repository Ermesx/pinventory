using System.Security.Claims;

using FluentResults;

using Microsoft.AspNetCore.Identity;

namespace Pinventory.Identity;

public static class Errors
{
    public static class TokenService
    {
        public static Error NoExternalLoginInfo() => new("No external login info found");

        public static Error UserNotFound(ClaimsPrincipal principal) => new($"User '{principal.Identity?.Name}' not found");

        public static IEnumerable<Error> SaveFailed(IEnumerable<IdentityResult> results) =>
            results.Where(r => !r.Succeeded).SelectMany(r => r.Errors).Select(e => new Error(e.Description));
    }
}