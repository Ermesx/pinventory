using FluentResults;

using Microsoft.AspNetCore.Authentication;

namespace Pinventory.Web.Google.Consent;

public interface IGoogleAuthStateService
{
    string CreateAndStoreState(AuthenticationProperties userData, TimeSpan? lifetime = null);

    Result<AuthenticationProperties> ValidateAndClearState(string stateFromQuery);

    void ClearStateCookie();
}