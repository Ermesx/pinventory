using Microsoft.Extensions.Options;

using Pinventory.ServiceDefaults;
using Pinventory.Web.Google;

namespace Pinventory.Web.Authorization;

public class AdminAuthorizationService(
    IGoogleUserService googleUserService,
    IOptions<PinventoryOptions> options,
    ILogger<AdminAuthorizationService> logger)
    : IAdminAuthorizationService
{
    public async Task<bool> IsCurrentUserAdminAsync()
    {
        var googleUserId = await googleUserService.GetGoogleUserIdAsync();
        if (googleUserId is null)
        {
            logger.LogWarning("No Google user ID found for current user");
            return false;
        }

        return googleUserId == options.Value.AdminId;
    }
}