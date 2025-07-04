using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Services.Interfaces;

namespace idc.pefindo.pbk.Services;

/// <summary>
/// Implementation of cycle day validation business logic
/// </summary>
public class CycleDayValidationService : ICycleDayValidationService
{
    private readonly IGlobalConfigRepository _globalConfigRepository;
    private readonly ILogger<CycleDayValidationService> _logger;

    public CycleDayValidationService(
        IGlobalConfigRepository globalConfigRepository,
        ILogger<CycleDayValidationService> logger)
    {
        _globalConfigRepository = globalConfigRepository;
        _logger = logger;
    }

    public async Task<bool> ValidateCycleDayAsync(int tolerance = 0)
    {
        try
        {
            // Retrieve cycle day configuration from database
            var cycleDayConfig = await GetCurrentCycleDayConfigAsync();
            
            if (!int.TryParse(cycleDayConfig, out int configuredCycleDay))
            {
                _logger.LogWarning("Invalid cycle day configuration: {CycleDayConfig}", cycleDayConfig);
                return false;
            }

            var currentDay = DateTime.UtcNow.Day;
            var difference = Math.Abs(currentDay - configuredCycleDay);
            var isValid = difference <= tolerance;
            
            _logger.LogInformation(
                "Cycle day validation - Current: {CurrentDay}, Configured: {ConfiguredDay}, Tolerance: {Tolerance}, Valid: {IsValid}",
                currentDay, configuredCycleDay, tolerance, isValid);

            return isValid;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating cycle day");
            return false; // Fail closed for security
        }
    }

    public async Task<string> GetCurrentCycleDayConfigAsync()
    {
        var configValue = await _globalConfigRepository.GetConfigValueAsync(GlobalConfigKeys.CycleDay);
        return configValue ?? "7"; // Default fallback
    }
}
