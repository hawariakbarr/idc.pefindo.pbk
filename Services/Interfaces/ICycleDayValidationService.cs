namespace idc.pefindo.pbk.Services.Interfaces;

/// <summary>
/// Service for validating cycle day business rules
/// </summary>
public interface ICycleDayValidationService
{
    /// <summary>
    /// Validates cycle day with PDP security checks using identity data
    /// </summary>
    /// <param name="idType">Identity type (e.g., KTP)</param>
    /// <param name="idNo">Identity number</param>
    /// <param name="tolerance">Number of days tolerance allowed (optional)</param>
    /// <returns>True if valid, false otherwise</returns>
    Task<bool> ValidateCycleDayWithPDPAsync(string idType, string idNo, int? tolerance = null);

    /// <summary>
    /// Retrieves the current cycle day configuration
    /// </summary>
    /// <returns>Cycle day configuration value</returns>
    Task<string> GetCurrentCycleDayConfigAsync();
}
