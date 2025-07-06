namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service for managing Pefindo API token lifecycle with caching
/// </summary>
public interface ITokenManagerService
{
    /// <summary>
    /// Gets a valid token, refreshing if necessary
    /// </summary>
    /// <returns>Valid access token</returns>
    Task<string> GetValidTokenAsync();
    
    /// <summary>
    /// Invalidates the current cached token
    /// </summary>
    Task InvalidateTokenAsync();
    
    /// <summary>
    /// Checks if current token is still valid
    /// </summary>
    /// <returns>True if token is valid</returns>
    Task<bool> IsTokenValidAsync();
}
