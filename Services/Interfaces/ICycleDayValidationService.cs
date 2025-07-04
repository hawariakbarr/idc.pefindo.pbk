namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service for validating cycle day business rules
/// </summary>
public interface ICycleDayValidationService
{
    /// <summary>
    /// Validates if the current request can be processed based on cycle day rules
    /// </summary>
    /// <param name="tolerance">Number of days tolerance allowed</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateCycleDayAsync(int tolerance = 0);
    
    /// <summary>
    /// Retrieves the current cycle day configuration
    /// </summary>
    /// <returns>Cycle day configuration value</returns>
    Task<string> GetCurrentCycleDayConfigAsync();
}
