using idc.pefindo.pbk.Models;

namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Updated token manager service interface for in-memory caching
/// </summary>
public interface ITokenManagerService
{
    /// <summary>
    /// Gets a valid token from cache or requests a new one
    /// </summary>
    Task<string> GetValidTokenAsync();

    /// <summary>
    /// Invalidates the current cached token
    /// </summary>
    Task InvalidateTokenAsync();

    /// <summary>
    /// Checks if the current cached token is valid
    /// </summary>
    Task<bool> IsTokenValidAsync();

    /// <summary>
    /// Gets token cache information for monitoring
    /// </summary>
    TokenCacheEntry? GetTokenCacheInfo();

    /// <summary>
    /// Forces refresh of the token
    /// </summary>
    Task<string> RefreshTokenAsync();
}
