namespace Pinventory.Web.Authorization;

public interface IAdminAuthorizationService
{
    Task<bool> IsCurrentUserAdminAsync();
}