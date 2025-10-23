using Microsoft.AspNetCore.Authorization;

using Pinventory.ApiDefaults;

namespace Pinventory.Pins.Api.Tags;

public record OwnerMatchesUserRequirement : IAuthorizationRequirement;

public class OwnerMatchesUserAuthorizationHandler() : AuthorizationHandler<OwnerMatchesUserRequirement, HttpContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerMatchesUserRequirement requirement,
        HttpContext httpContext)
    {
        var userId = context.User.GetIdentifier();

        if (httpContext.GetRouteValue("ownerId") is not string ownerId || ownerId == userId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

