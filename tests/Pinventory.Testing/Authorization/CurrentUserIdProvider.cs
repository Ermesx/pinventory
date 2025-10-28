namespace Pinventory.Testing.Authorization;

public class CurrentUserIdProvider
{
    public string CurrentUserId { get; set; } = AuthenticationTestHandler.TestUserId;
}