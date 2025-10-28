namespace Pinventory.Web.Google;

public interface IGoogleUserService
{
    Task<string?> GetGoogleUserIdAsync();
}