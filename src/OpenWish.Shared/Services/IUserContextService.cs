namespace OpenWish.Shared.Services;

public interface IUserContextService
{
    Task<string?> GetUserIdAsync();
    Task<string?> GetUserNameAsync();
    Task<bool> IsAuthenticatedAsync();
}