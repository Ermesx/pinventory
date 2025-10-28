using Microsoft.Extensions.Options;

using Pinventory.ServiceDefaults;
using Pinventory.Web.Google;

namespace Pinventory.Web.Authorization;

public class AdminAuthorizationService(IGoogleUserService googleUserService, IOptions<PinventoryOptions> options)
    : IAdminAuthorizationService
{
    public async Task<bool> IsCurrentUserAdminAsync()
    {
        var googleUserId = await googleUserService.GetGoogleUserIdAsync();
        return googleUserId == options.Value.AdminId;
    }
}