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

    public async Task<bool> ValidateCycleDayWithPDPAsync(string idType, string idNo, int? tolerance = null)
    {
        try
        {
            var (firstTrue, secondTrue) = (false, false);
            _logger.LogInformation("Starting PDP cycle day validation for id_type: {IdType}, id_no: {IdNo}", idType, idNo);

            // Use tolerance from parameter or get from config
            int cycleDayConfigValue = tolerance ?? await GetToleranceFromConfigAsync();

            var decryptedSymmetricKey = _encryptionService.DecryptString(_pdpConfig.SymmetricKey);
            var pbkInfo = await _pbkDataRepository.GetPbkInfoIdentityWithEncryptionAsync(idType, idNo, decryptedSymmetricKey);
            if (pbkInfo != null)
            {
                var referenceDate = pbkInfo.PfReqDate.AddDays(cycleDayConfigValue);
                var currentDate = DateTime.UtcNow;
                // Check if current date is within the cycle day period (before reference date)
                // If true, use cached data; if false, fetch fresh data from Pefindo PBK API
                var isValid = currentDate < referenceDate;

                _logger.LogInformation(
                    "PBK info cycle day validation - PfReqDate: {PfReqDate}, ReferenceDate: {ReferenceDate}, CurrentDate: {CurrentDate}, Tolerance: {Tolerance}, Valid: {IsValid}",
                    pbkInfo.PfReqDate, referenceDate, currentDate, cycleDayConfigValue, isValid);

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
                var currentDate = DateTime.UtcNow;
                // Check if current date is within the cycle day period (before reference date)
                // If true, use cached data; if false, fetch fresh data from Pefindo PBK API
                var isValid = currentDate < referenceDate;

                _logger.LogInformation(
                    "Summary perorangan cycle day validation - IspCreatedDate: {IspCreatedDate}, ReferenceDate: {ReferenceDate}, CurrentDate: {CurrentDate}, Tolerance: {Tolerance}, Valid: {IsValid}",
                    summaryInfo.IspCreatedDate, referenceDate, currentDate, cycleDayConfigValue, isValid);

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
