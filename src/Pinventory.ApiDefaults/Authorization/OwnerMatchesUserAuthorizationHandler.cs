using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace Pinventory.ApiDefaults.Authorization;

public record OwnerMatchesUserRequirement(string AdminId) : IAuthorizationRequirement;

public class OwnerMatchesUserAuthorizationHandler() : AuthorizationHandler<OwnerMatchesUserRequirement, HttpContext>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OwnerMatchesUserRequirement requirement,
        HttpContext httpContext)
    {
        var userId = context.User.GetIdentifier();
        var ownerId = httpContext.GetRouteValue("ownerId") as string;

        if (ownerId is not null && userId == ownerId)
        {
            context.Succeed(requirement);
        }

        // Only modifying a global catalog requires admin
        var method = httpContext.Request.Method;
        if (ownerId is null && (userId == requirement.AdminId || method == HttpMethods.Get))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}