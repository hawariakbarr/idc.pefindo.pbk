namespace idc.pefindo.pbk.DataAccess;

/// <summary>
/// Repository for managing global configuration data
/// </summary>
public interface IGlobalConfigRepository
{
    /// <summary>
    /// Retrieves a single configuration value by code
    /// </summary>
    /// <param name="configCode">Configuration code to retrieve</param>
    /// <returns>Configuration value or null if not found</returns>
    Task<string?> GetConfigValueAsync(string configCode);
    
    /// <summary>
    /// Retrieves multiple configuration values by codes
    /// </summary>
    /// <param name="configCodes">Array of configuration codes</param>
    /// <returns>Dictionary of config code to value mappings</returns>
    Task<Dictionary<string, string>> GetMultipleConfigsAsync(params string[] configCodes);
}
