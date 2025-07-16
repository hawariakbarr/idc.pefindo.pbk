using idc.pefindo.pbk.Configuration;
using idc.pefindo.pbk.DataAccess;
using idc.pefindo.pbk.Services.Interfaces;
using Microsoft.Extensions.Options;
using EncryptionApi.Services;


namespace idc.pefindo.pbk.Services;

/// <summary>
/// Implementation of cycle day validation business logic with PDP security support
/// </summary>
public class CycleDayValidationService : ICycleDayValidationService
{
    private readonly IGlobalConfigRepository _globalConfigRepository;
    private readonly IPbkDataRepository _pbkDataRepository;
    private readonly ILogger<CycleDayValidationService> _logger;
    private readonly GlobalConfig _globalConfig;
    private readonly PDPConfig _pdpConfig;
    private readonly IEncryptionService _encryptionService;

    public CycleDayValidationService(
        IGlobalConfigRepository globalConfigRepository,
        IPbkDataRepository pbkDataRepository,
        ILogger<CycleDayValidationService> logger,
        IOptions<GlobalConfig> globalConfigOptions,
        IOptions<PDPConfig> pdpConfigOptions,
        IEncryptionService encryptionService)
    {
        _globalConfigRepository = globalConfigRepository;
        _pbkDataRepository = pbkDataRepository;
        _logger = logger;
        _globalConfig = globalConfigOptions.Value;
        _pdpConfig = pdpConfigOptions.Value;
        _encryptionService = encryptionService;
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

    public async Task<bool> ValidateCycleDayWithPDPAsync(string idType, string idNo, int? tolerance = null)
    {
        try
        {
            var (firstTrue, secondTrue) = (false, false);
            _logger.LogInformation("Starting PDP cycle day validation for id_type: {IdType}, id_no: {IdNo}", idType, idNo);

            // Use tolerance from parameter or get from config
            int cycleDayConfigValue = tolerance ?? await GetToleranceFromConfigAsync();

            // Check if PDP is active
            if (!_pdpConfig.IsActive)
            {
                _logger.LogInformation("PDP is not active, falling back to standard cycle day validation");
                return await ValidateCycleDayAsync(cycleDayConfigValue);
            }

            // // Validate PDP configuration
            // if (!_pdpConfig.IsValid())
            // {
            //     _logger.LogWarning("PDP configuration is invalid, falling back to standard cycle day validation");
            //     return await ValidateCycleDayAsync(actualTolerance);
            // }

            // Get cycle day configuration
            // var cycleDayConfig = await GetCurrentCycleDayConfigAsync();
            // if (!int.TryParse(cycleDayConfig, out int cycleDayConfigValue))
            // {
            //     _logger.LogWarning("Invalid cycle day configuration: {CycleDayConfig}", cycleDayConfig);
            //     return false;
            // }

            var decryptedSymmetricKey = _encryptionService.DecryptString(_pdpConfig.SymmetricKey);
            var pbkInfo = await _pbkDataRepository.GetPbkInfoIdentityWithEncryptionAsync(idType, idNo, decryptedSymmetricKey);
            if (pbkInfo != null)
            {
                var referenceDate = pbkInfo.PfReqDate.AddDays(cycleDayConfigValue);
                var currentDay = DateTime.UtcNow.Day;
                var difference = Math.Abs(currentDay - referenceDate.Day);
                var isValid = difference <= referenceDate.Day; ;

                _logger.LogInformation(
                    "PBK info cycle day validation - PfReqDate: {PfReqDate}, ReferenceDate: {ReferenceDate}, CurrentDay: {CurrentDay}, Tolerance: {Tolerance}, Valid: {IsValid}",
                    pbkInfo.PfReqDate, referenceDate, currentDay, cycleDayConfigValue, isValid);

                if (isValid)
                {
                    firstTrue = true;
                    _logger.LogInformation("PBK info cycle day validation passed for id_type: {IdType}, id_no: {IdNo}", idType, idNo);
                }
            }

            // Second check: Summary perorangan identity (using KTP)
            var summaryInfo = await _pbkDataRepository.GetSummaryPeroranganIdentityWithEncryptionAsync(idNo, decryptedSymmetricKey);
            if (summaryInfo != null)
            {
                var referenceDate = summaryInfo.IspCreatedDate.AddDays(cycleDayConfigValue);
                var currentDay = DateTime.UtcNow.Day;
                var difference = Math.Abs(currentDay - referenceDate.Day);
                var isValid = difference <= referenceDate.Day;

                _logger.LogInformation(
                    "Summary perorangan cycle day validation - IspCreatedDate: {IspCreatedDate}, ReferenceDate: {ReferenceDate}, CurrentDay: {CurrentDay}, Tolerance: {Tolerance}, Valid: {IsValid}",
                    summaryInfo.IspCreatedDate, referenceDate, currentDay, cycleDayConfigValue, isValid);

                if (isValid)
                {
                    secondTrue = true;
                    _logger.LogInformation("Summary perorangan cycle day validation passed for id_type: {IdType}, id_no: {IdNo}", idType, idNo);
                }
            }

            // If any check passed, validation true
            if (firstTrue || secondTrue)
            {
                // Both checks passed
                _logger.LogInformation("Both PDP cycle day validation checks passed for id_type: {IdType}, id_no: {IdNo}", idType, idNo);
                return true;
            }

            // If both checks failed
            _logger.LogInformation("PDP cycle day validation failed for id_type: {IdType}, id_no: {IdNo}. FirstCheck: {FirstTrue}, SecondCheck: {SecondTrue}", idType, idNo, firstTrue, secondTrue);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in PDP cycle day validation for id_type: {IdType}, id_no: {IdNo}", idType, idNo);
            return false; // Fail closed for security
        }
    }

    public async Task<string> GetCurrentCycleDayConfigAsync()
    {
        var configValue = await _globalConfigRepository.GetConfigValueAsync(_globalConfig.CycleDay);
        return configValue ?? "30"; // Default fallback
    }

    private async Task<int> GetToleranceFromConfigAsync()
    {
        var cycleDayConfig = await GetCurrentCycleDayConfigAsync();
        return int.TryParse(cycleDayConfig, out int tolerance) ? tolerance : 30;
    }
}
